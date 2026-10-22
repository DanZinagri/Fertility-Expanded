using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace FertilityExpanded.Patches.Compatibility;

internal static class FamilyProtectionCompat
{
    private const string AlreadyPlacedTranslationKey = "FE_FamilyIsEverythingAlreadyPlaced";
    private const string ChildAssailedThoughtDefName = "ChildAssailed";
    private const string ParentAssailedThoughtDefName = "ParentAssailed";

    private static FieldInfo protectChildSettingField;
    private static FieldInfo involveOtherFactionsSettingField;
    private static Action<Pawn, Pawn, Pawn> protectChildAction;
    private static ThoughtDef childAssailedThought;
    private static ThoughtDef parentAssailedThought;

    public sealed class AdoptionState
    {
        public AdoptionState(Pawn child, Pawn father, Pawn mother, bool fatherWasParent, bool motherWasParent)
        {
            Child = child;
            Father = father;
            Mother = mother;
            FatherWasParent = fatherWasParent;
            MotherWasParent = motherWasParent;
        }

        public Pawn Child { get; }

        public Pawn Father { get; }

        public Pawn Mother { get; }

        public bool FatherWasParent { get; }

        public bool MotherWasParent { get; }
    }

    public static bool Initialize()
    {
        if (!CanUseFamilyRelations)
        {
            return false;
        }

        Type settingsType = AccessTools.TypeByName("CrycoreFamilyisEverything.DTMF_ModSettings");
        protectChildSettingField = settingsType == null
            ? null
            : AccessTools.Field(settingsType, "ProtectChildWhenGetAssaulted");
        involveOtherFactionsSettingField = settingsType == null
            ? null
            : AccessTools.Field(settingsType, "InvolveOtherFactions");
        if (involveOtherFactionsSettingField?.FieldType != typeof(bool)
            || !involveOtherFactionsSettingField.IsStatic)
        {
            involveOtherFactionsSettingField = null;
        }

        Type damagePatchType = AccessTools.TypeByName("CrycoreFamilyisEverything.PostPreApplyDamage_Patch");
        MethodInfo protectChildMethod = damagePatchType == null
            ? null
            : AccessTools.Method(damagePatchType, "ProtectChild", new[] { typeof(Pawn), typeof(Pawn), typeof(Pawn) });
        protectChildAction = protectChildMethod == null
            ? null
            : (Action<Pawn, Pawn, Pawn>)Delegate.CreateDelegate(typeof(Action<Pawn, Pawn, Pawn>), protectChildMethod);

        childAssailedThought = DefDatabase<ThoughtDef>.GetNamedSilentFail(ChildAssailedThoughtDefName);
        parentAssailedThought = DefDatabase<ThoughtDef>.GetNamedSilentFail(ParentAssailedThoughtDefName);

        if (protectChildSettingField == null)
        {
            Log.Warning("[Fertility Expanded] Could not find the Family Is Everything child-protection setting. Adoptive and foster memories will still be integrated.");
        }

        if (involveOtherFactionsSettingField == null)
        {
            Log.Warning("[Fertility Expanded] Could not find the Family Is Everything faction-involvement setting. Cross-faction adoptive and foster reactions will be disabled.");
        }

        if (protectChildAction == null)
        {
            Log.Warning("[Fertility Expanded] Could not find the Family Is Everything child-protection method. Adoptive and foster memories will still be integrated.");
        }

        if (childAssailedThought == null || parentAssailedThought == null)
        {
            Log.Warning("[Fertility Expanded] Could not find the Family Is Everything family-assault thoughts. Protective behavior will still be integrated.");
        }

        return true;
    }

    public static void AdopteeAppliesToPawnPostfix(Pawn p, ref string reason, ref bool __result)
    {
        if (!__result || !HasFertilityExpandedParent(p))
        {
            return;
        }

        __result = false;
        reason = AlreadyPlacedTranslationKey.Translate(p.Named("CHILD")).Resolve();
    }

    public static bool AdoptionApplyPrefix(LordJob_Ritual jobRitual, out AdoptionState __state)
    {
        __state = null;
        if (jobRitual == null)
        {
            return true;
        }

        Pawn father = jobRitual.PawnWithRole("father");
        Pawn mother = jobRitual.PawnWithRole("mother");
        Pawn child = jobRitual.PawnWithRole("adoptee");
        if (child?.relations == null || father == null || mother == null)
        {
            return true;
        }

        if (HasFertilityExpandedParent(child))
        {
            return false;
        }

        __state = new AdoptionState(
            child,
            father,
            mother,
            HasVanillaParentRelation(child, father),
            HasVanillaParentRelation(child, mother));
        return true;
    }

    public static void AdoptionApplyPostfix(AdoptionState __state)
    {
        if (__state?.Child?.relations == null)
        {
            return;
        }

        bool fatherRelationCreated = !__state.FatherWasParent
            && HasVanillaParentRelation(__state.Child, __state.Father);
        bool motherRelationCreated = !__state.MotherWasParent
            && HasVanillaParentRelation(__state.Child, __state.Mother);
        if (!fatherRelationCreated && !motherRelationCreated)
        {
            return;
        }

        if (!__state.FatherWasParent)
        {
            ConvertRitualParent(__state.Child, __state.Father, fatherRelationCreated);
        }

        if (!__state.MotherWasParent)
        {
            ConvertRitualParent(__state.Child, __state.Mother, motherRelationCreated);
        }
    }

    public static void MakeFamilyMembersKnownPostfix(Pawn pawnIns, Pawn target)
    {
        if (!CanUseFamilyRelations
            || pawnIns?.relations == null
            || target == null
            || !AllowsFamilyResponse(pawnIns, target))
        {
            return;
        }

        Pawn protector = NotifyFertilityExpandedParents(pawnIns, target);
        NotifyFertilityExpandedChildren(pawnIns, target);

        if (protector != null
            && !OriginalHasOnMapParentProtector(pawnIns)
            && ProtectChildWhenAssaulted)
        {
            protectChildAction?.Invoke(protector, target, pawnIns);
        }
    }

    private static bool AllowsFamilyResponse(Pawn victim, Pawn attacker)
    {
        if (victim.Faction == attacker.Faction)
        {
            return true;
        }

        return involveOtherFactionsSettingField?.GetValue(null) is bool enabled
            && enabled
            && victim.Faction != null
            && attacker.Faction != null
            && attacker.Faction.RelationKindWith(victim.Faction) != FactionRelationKind.Hostile;
    }

    private static void ConvertRitualParent(Pawn child, Pawn parent, bool removeVanillaParent)
    {
        if (!ParentAssignment.TryAssignExternalAdoptiveParent(child, parent))
        {
            return;
        }

        if (removeVanillaParent)
        {
            child.relations.TryRemoveDirectRelation(PawnRelationDefOf.Parent, parent);
        }
    }

    private static Pawn NotifyFertilityExpandedParents(Pawn child, Pawn attacker)
    {
        HashSet<Pawn> seen = new HashSet<Pawn>();
        Pawn strongestParent = null;
        int strongestMelee = -1;

        List<DirectPawnRelation> relations = child.relations.DirectRelations;
        for (int i = 0; i < relations.Count; i++)
        {
            DirectPawnRelation relation = relations[i];
            Pawn parent = relation.otherPawn;
            if (!FamilyRelations.IsNonVanillaParentRelation(relation.def)
                || parent == null
                || parent.Dead
                || parent.Map != child.Map
                || !seen.Add(parent)
                || HasVanillaParentRelation(child, parent))
            {
                continue;
            }

            if (childAssailedThought != null
                && parent.needs?.mood?.thoughts?.memories != null)
            {
                parent.needs.mood.thoughts.memories.TryGainMemory(childAssailedThought, attacker);
            }

            int meleeLevel = parent.skills?.GetSkill(SkillDefOf.Melee)?.Level ?? -1;
            if (strongestParent == null || meleeLevel > strongestMelee)
            {
                strongestParent = parent;
                strongestMelee = meleeLevel;
            }
        }

        return strongestParent;
    }

    private static void NotifyFertilityExpandedChildren(Pawn parent, Pawn attacker)
    {
        if (parentAssailedThought == null)
        {
            return;
        }

        HashSet<Pawn> seen = new HashSet<Pawn>();
        foreach (Pawn child in parent.relations.PotentiallyRelatedPawns)
        {
            if (child?.relations == null
                || child.Dead
                || child.Map != parent.Map
                || !seen.Add(child)
                || HasVanillaParentRelation(child, parent)
                || !FamilyRelations.HasParentLikeRelation(child, parent)
                || child.needs?.mood?.thoughts?.memories == null)
            {
                continue;
            }

            child.needs.mood.thoughts.memories.TryGainMemory(parentAssailedThought, attacker);
        }
    }

    private static bool HasFertilityExpandedParent(Pawn child)
    {
        if (child?.relations == null)
        {
            return false;
        }

        List<DirectPawnRelation> relations = child.relations.DirectRelations;
        for (int i = 0; i < relations.Count; i++)
        {
            if (FamilyRelations.IsNonVanillaParentRelation(relations[i].def))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasVanillaParentRelation(Pawn child, Pawn parent)
    {
        return child?.relations?.DirectRelationExists(PawnRelationDefOf.Parent, parent) == true;
    }

    private static bool OriginalHasOnMapParentProtector(Pawn child)
    {
        Pawn father = ParentRelationUtility.GetFather(child);
        Pawn mother = ParentRelationUtility.GetMother(child);
        if (father != null && mother != null)
        {
            return father.Map == child.Map && mother.Map == child.Map;
        }

        if (father != null)
        {
            return father.Map == child.Map;
        }

        return mother != null && mother.Map == child.Map;
    }

    private static bool ProtectChildWhenAssaulted
    {
        get
        {
            return protectChildSettingField?.GetValue(null) is bool enabled && enabled;
        }
    }

    private static bool CanUseFamilyRelations => FE_DefOf.FE_AdoptiveParent != null
        && FE_DefOf.FE_FosterParent != null;
}
