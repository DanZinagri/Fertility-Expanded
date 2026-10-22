using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace FertilityExpanded.Patches.Compatibility;

internal static class FamilyTiesCompat
{
    private static object settings;
    private static FieldInfo ageOfCaringField;
    private static FieldInfo childrenWorryAboutParentsField;
    private static FieldInfo childMinEmpathyAgeField;
    private static FieldInfo childMaxEmpathyAgeField;
    private static FieldInfo proudForMasterpieceField;
    private static FieldInfo proudForSkillUpField;
    private static FieldInfo thoughtPawnField;

    private static Func<Pawn, Pawn, bool> hasGoodRelation;
    private static Func<Pawn, bool, bool> isUnempathetic;
    private static Func<Pawn, bool> isSick;
    private static Func<Pawn, bool> isCriticallyIll;
    private static Func<Pawn, float, bool> isWorrisomeDiseaseAboveThreshold;
    private static Func<Pawn, float> getPainThreshold;
    private static Func<Pawn, float> getSicknessThreshold;

    private static ThoughtDef harmedMyOwnChild;
    private static ThoughtDef harmedMyChild;
    private static ThoughtDef mySonLostLimb;
    private static ThoughtDef myDaughterLostLimb;
    private static ThoughtDef myChildCreatedMasterpiece;
    private static ThoughtDef myChildLeveledUpSkill;

    public static bool Initialize()
    {
        Type modType = AccessTools.TypeByName("FamilyTies.FamilyTiesMod");
        Type settingsType = AccessTools.TypeByName("FamilyTies.FamilyTiesSettings");
        Type relationUtilityType = AccessTools.TypeByName("FamilyTies.PawnRelationUtil");
        Type traitUtilityType = AccessTools.TypeByName("FamilyTies.TraitUtil");
        Type sickPawnUtilityType = AccessTools.TypeByName("FamilyTies.SickPawnUtil");
        Type stoicismUtilityType = AccessTools.TypeByName("FamilyTies.StoicismUtil");
        if (modType == null
            || settingsType == null
            || relationUtilityType == null
            || traitUtilityType == null
            || sickPawnUtilityType == null
            || stoicismUtilityType == null)
        {
            Log.Warning("[Fertility Expanded] Family Ties is active, but its expected compatibility types were not found.");
            return false;
        }

        FieldInfo settingsField = AccessTools.Field(modType, "settings");
        settings = settingsField?.GetValue(null);
        ageOfCaringField = AccessTools.Field(settingsType, "ageOfCaring");
        childrenWorryAboutParentsField = AccessTools.Field(settingsType, "childrenWorryAboutParents");
        childMinEmpathyAgeField = AccessTools.Field(settingsType, "childMinEmpathyAge");
        childMaxEmpathyAgeField = AccessTools.Field(settingsType, "childMaxEmpathyAge");
        proudForMasterpieceField = AccessTools.Field(settingsType, "proudForMasterpiece");
        proudForSkillUpField = AccessTools.Field(settingsType, "proudForSkillUp");
        thoughtPawnField = AccessTools.Field(typeof(Thought), "pawn");

        MethodInfo hasGoodRelationMethod = AccessTools.Method(
            relationUtilityType,
            "HasGoodRelation",
            new[] { typeof(Pawn), typeof(Pawn) });
        MethodInfo isUnempatheticMethod = AccessTools.Method(
            traitUtilityType,
            "IsUnempathetic",
            new[] { typeof(Pawn), typeof(bool) });
        MethodInfo isSickMethod = AccessTools.Method(sickPawnUtilityType, "IsSick", new[] { typeof(Pawn) });
        MethodInfo isCriticallyIllMethod = AccessTools.Method(sickPawnUtilityType, "IsCriticallyIll", new[] { typeof(Pawn) });
        MethodInfo isWorrisomeDiseaseAboveThresholdMethod = AccessTools.Method(
            sickPawnUtilityType,
            "IsWorrisomeDiseaseAboveThreshold",
            new[] { typeof(Pawn), typeof(float) });
        MethodInfo getPainThresholdMethod = AccessTools.Method(stoicismUtilityType, "GetPainThreshold", new[] { typeof(Pawn) });
        MethodInfo getSicknessThresholdMethod = AccessTools.Method(stoicismUtilityType, "GetSicknessThreshold", new[] { typeof(Pawn) });

        if (settings == null
            || ageOfCaringField == null
            || childrenWorryAboutParentsField == null
            || childMinEmpathyAgeField == null
            || childMaxEmpathyAgeField == null
            || proudForMasterpieceField == null
            || proudForSkillUpField == null
            || thoughtPawnField == null
            || hasGoodRelationMethod == null
            || isUnempatheticMethod == null
            || isSickMethod == null
            || isCriticallyIllMethod == null
            || isWorrisomeDiseaseAboveThresholdMethod == null
            || getPainThresholdMethod == null
            || getSicknessThresholdMethod == null)
        {
            Log.Warning("[Fertility Expanded] Family Ties is active, but its expected settings or utility methods were not found.");
            return false;
        }

        hasGoodRelation = (Func<Pawn, Pawn, bool>)Delegate.CreateDelegate(
            typeof(Func<Pawn, Pawn, bool>),
            hasGoodRelationMethod);
        isUnempathetic = (Func<Pawn, bool, bool>)Delegate.CreateDelegate(
            typeof(Func<Pawn, bool, bool>),
            isUnempatheticMethod);
        isSick = (Func<Pawn, bool>)Delegate.CreateDelegate(typeof(Func<Pawn, bool>), isSickMethod);
        isCriticallyIll = (Func<Pawn, bool>)Delegate.CreateDelegate(typeof(Func<Pawn, bool>), isCriticallyIllMethod);
        isWorrisomeDiseaseAboveThreshold = (Func<Pawn, float, bool>)Delegate.CreateDelegate(
            typeof(Func<Pawn, float, bool>),
            isWorrisomeDiseaseAboveThresholdMethod);
        getPainThreshold = (Func<Pawn, float>)Delegate.CreateDelegate(typeof(Func<Pawn, float>), getPainThresholdMethod);
        getSicknessThreshold = (Func<Pawn, float>)Delegate.CreateDelegate(typeof(Func<Pawn, float>), getSicknessThresholdMethod);

        harmedMyOwnChild = DefDatabase<ThoughtDef>.GetNamedSilentFail("HarmedMyOwnChild");
        harmedMyChild = DefDatabase<ThoughtDef>.GetNamedSilentFail("HarmedMyChild");
        mySonLostLimb = DefDatabase<ThoughtDef>.GetNamedSilentFail("MySonLostLimb");
        myDaughterLostLimb = DefDatabase<ThoughtDef>.GetNamedSilentFail("MyDaughterLostLimb");
        myChildCreatedMasterpiece = DefDatabase<ThoughtDef>.GetNamedSilentFail("MyChildCreatedMasterpiece");
        myChildLeveledUpSkill = DefDatabase<ThoughtDef>.GetNamedSilentFail("MyChildLeveledUpSkill");
        if (harmedMyOwnChild == null
            || harmedMyChild == null
            || mySonLostLimb == null
            || myDaughterLostLimb == null
            || myChildCreatedMasterpiece == null
            || myChildLeveledUpSkill == null)
        {
            Log.Warning("[Fertility Expanded] Family Ties is active, but one or more of its event thoughts were not found.");
        }

        return true;
    }

    public static void TakeDamagePostfix(Thing __instance, DamageInfo dinfo)
    {
        if (__instance is not Pawn child
            || dinfo.Amount <= 0f
            || dinfo.Instigator is not Pawn attacker
            || attacker == child
            || child.RaceProps?.Humanlike != true
            || !ChildWithinCaringAge(child))
        {
            return;
        }

        foreach (Pawn parent in NonVanillaOnlyParentsOf(child))
        {
            if (parent.Dead || !hasGoodRelation(parent, child) || isUnempathetic(parent, false))
            {
                continue;
            }

            GainMemory(
                parent,
                parent == attacker ? harmedMyOwnChild : harmedMyChild,
                parent == attacker ? null : attacker);
        }
    }

    public static void AddHediffPostfix(Hediff hediff)
    {
        if (hediff is not Hediff_MissingPart || hediff.pawn is not Pawn child || !ChildWithinCaringAge(child))
        {
            return;
        }

        ThoughtDef thought = child.gender == Gender.Male ? mySonLostLimb : myDaughterLostLimb;
        foreach (Pawn parent in NonVanillaOnlyParentsOf(child))
        {
            if (!parent.Dead && hasGoodRelation(parent, child) && !isUnempathetic(parent, false))
            {
                GainMemory(parent, thought, null);
            }
        }
    }

    public static void MasterpiecePostfix(Pawn child, Thing product)
    {
        if (child == null
            || product == null
            || !ProudForMasterpiece
            || !QualityUtility.TryGetQuality(product, out QualityCategory quality)
            || (quality != QualityCategory.Masterwork && quality != QualityCategory.Legendary)
            || !ChildWithinCaringAge(child))
        {
            return;
        }

        foreach (Pawn parent in NonVanillaOnlyParentsOf(child))
        {
            if (!parent.Dead && hasGoodRelation(parent, child) && !isUnempathetic(parent, true))
            {
                GainMemory(parent, myChildCreatedMasterpiece, null);
            }
        }
    }

    public static void SkillLearnPrefix(SkillRecord __instance, out int __state)
    {
        __state = __instance.Level;
    }

    public static void SkillLearnPostfix(SkillRecord __instance, int __state, Pawn ___pawn)
    {
        Pawn child = ___pawn;
        if (!ProudForSkillUp
            || __instance.Level <= __state
            || child == null
            || !ChildWithinCaringAge(child))
        {
            return;
        }

        foreach (Pawn parent in NonVanillaOnlyParentsOf(child))
        {
            if (!parent.Dead && hasGoodRelation(parent, child) && !isUnempathetic(parent, true))
            {
                GainMemory(parent, myChildLeveledUpSkill, null);
            }
        }
    }

    public static void MyChildIsInPainPostfix(Pawn p, ref ThoughtState __result)
    {
        if (p?.Map == null || isUnempathetic(p, false))
        {
            return;
        }

        int additionalCount = 0;
        foreach (Pawn child in NonVanillaOnlyChildrenOnMap(p))
        {
            if (hasGoodRelation(p, child)
                && ChildWithinCaringAge(child)
                && !child.Dead
                && child.health.hediffSet.PainTotal > 0.01f)
            {
                additionalCount++;
            }
        }

        AddCountToState(ref __result, additionalCount, 0, 1);
    }

    public static void MyChildIsSickPostfix(Pawn p, ref ThoughtState __result)
    {
        if (p?.Map == null || isUnempathetic(p, false))
        {
            return;
        }

        int additionalSickCount = 0;
        int additionalCriticalCount = 0;
        foreach (Pawn child in NonVanillaOnlyChildrenOnMap(p))
        {
            if (!hasGoodRelation(p, child) || !ChildWithinCaringAge(child) || !isSick(child))
            {
                continue;
            }

            additionalSickCount++;
            if (isCriticallyIll(child))
            {
                additionalCriticalCount++;
            }
        }

        if (additionalSickCount == 0)
        {
            return;
        }

        if (!__result.Active)
        {
            __result = SickChildState(additionalSickCount, additionalCriticalCount);
            return;
        }

        if (__result.StageIndex == 0 || __result.StageIndex == 2)
        {
            __result = ThoughtState.ActiveAtStage(2);
        }
        else if ((__result.StageIndex == 1 || __result.StageIndex == 3)
            && additionalCriticalCount != additionalSickCount)
        {
            __result = ThoughtState.ActiveAtStage(2);
        }
        else if (__result.StageIndex == 1)
        {
            __result = ThoughtState.ActiveAtStage(3);
        }
    }

    public static void MyParentIsInPainPostfix(Pawn p, ref ThoughtState __result)
    {
        if (!CanWorryAboutParents(p))
        {
            return;
        }

        int additionalCount = 0;
        foreach (Pawn parent in NonVanillaOnlyParentsOf(p))
        {
            if (!parent.Dead
                && parent.Map == p.Map
                && hasGoodRelation(p, parent)
                && parent.health.hediffSet.PainTotal >= getPainThreshold(parent))
            {
                additionalCount++;
            }
        }

        AddCountToState(ref __result, additionalCount, 0, 1);
    }

    public static void MyParentIsSickPostfix(Pawn p, ref ThoughtState __result)
    {
        if (!CanWorryAboutParents(p))
        {
            return;
        }

        int additionalCount = 0;
        foreach (Pawn parent in NonVanillaOnlyParentsOf(p))
        {
            if (hasGoodRelation(parent, p)
                && isWorrisomeDiseaseAboveThreshold(parent, getSicknessThreshold(parent)))
            {
                additionalCount++;
            }
        }

        AddCountToState(ref __result, additionalCount, 0, 1);
    }

    public static void FindFirstSufferingChildPostfix(object __instance, ref Pawn __result)
    {
        if (__result != null || ThoughtPawn(__instance) is not Pawn parent || parent.Map == null)
        {
            return;
        }

        foreach (Pawn child in NonVanillaOnlyChildrenOnMap(parent))
        {
            if (hasGoodRelation(parent, child)
                && !child.Dead
                && child.health.hediffSet.PainTotal > 0.01f
                && ChildWithinCaringAge(child))
            {
                __result = child;
                return;
            }
        }
    }

    public static void FindSingleSickChildPostfix(object __instance, ref Pawn __result)
    {
        if (__result != null || ThoughtPawn(__instance) is not Pawn parent || parent.Map == null)
        {
            return;
        }

        Pawn found = null;
        int count = 0;
        foreach (Pawn child in NonVanillaOnlyChildrenOnMap(parent))
        {
            if (!hasGoodRelation(parent, child) || !isSick(child))
            {
                continue;
            }

            found = child;
            count++;
        }

        if (count == 1)
        {
            __result = found;
        }
    }

    public static void FindSingleSufferingParentPostfix(object __instance, ref Pawn __result)
    {
        Pawn child = ThoughtPawn(__instance);
        if (__result != null || child == null)
        {
            return;
        }

        Pawn found = null;
        int count = 0;
        foreach (Pawn parent in NonVanillaOnlyParentsOf(child))
        {
            if (hasGoodRelation(child, parent)
                && !parent.Dead
                && parent.Map == child.Map
                && parent.health.hediffSet.PainTotal > getPainThreshold(parent))
            {
                found = parent;
                count++;
            }
        }

        if (count == 1)
        {
            __result = found;
        }
    }

    public static void FindSingleSickParentPostfix(object __instance, ref Pawn __result)
    {
        Pawn child = ThoughtPawn(__instance);
        if (__result != null || child == null)
        {
            return;
        }

        Pawn found = null;
        int count = 0;
        foreach (Pawn parent in NonVanillaOnlyParentsOf(child))
        {
            if (hasGoodRelation(child, parent)
                && !parent.Dead
                && parent.Map == child.Map
                && isSick(parent))
            {
                found = parent;
                count++;
            }
        }

        if (count == 1)
        {
            __result = found;
        }
    }

    private static IEnumerable<Pawn> NonVanillaOnlyParentsOf(Pawn child)
    {
        if (child?.relations == null)
        {
            yield break;
        }

        HashSet<Pawn> seen = new HashSet<Pawn>();
        List<DirectPawnRelation> relations = child.relations.DirectRelations;
        for (int i = 0; i < relations.Count; i++)
        {
            DirectPawnRelation relation = relations[i];
            Pawn parent = relation.otherPawn;
            if (!FamilyRelations.IsNonVanillaParentRelation(relation.def)
                || parent == null
                || child.relations.DirectRelationExists(PawnRelationDefOf.Parent, parent)
                || !seen.Add(parent))
            {
                continue;
            }

            yield return parent;
        }
    }

    private static IEnumerable<Pawn> NonVanillaOnlyChildrenOnMap(Pawn parent)
    {
        if (parent?.Map == null || parent.relations == null)
        {
            yield break;
        }

        foreach (Pawn child in parent.relations.PotentiallyRelatedPawns)
        {
            if (child?.relations == null
                || child.Map != parent.Map
                || child.relations.DirectRelationExists(PawnRelationDefOf.Parent, parent)
                || !HasNonVanillaParentRelation(child, parent))
            {
                continue;
            }

            yield return child;
        }
    }

    private static bool HasNonVanillaParentRelation(Pawn child, Pawn parent)
    {
        List<DirectPawnRelation> relations = child.relations.DirectRelations;
        for (int i = 0; i < relations.Count; i++)
        {
            DirectPawnRelation relation = relations[i];
            if (relation.otherPawn == parent && FamilyRelations.IsNonVanillaParentRelation(relation.def))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ChildWithinCaringAge(Pawn child)
    {
        int ageOfCaring = AgeOfCaring;
        return child?.ageTracker != null
            && (ageOfCaring == 0 || child.ageTracker.AgeBiologicalYears <= ageOfCaring);
    }

    private static bool CanWorryAboutParents(Pawn child)
    {
        if (!ChildrenWorryAboutParents || child?.ageTracker == null || child.Map == null || isUnempathetic(child, false))
        {
            return false;
        }

        int age = child.ageTracker.AgeBiologicalYears;
        return age >= ChildMinEmpathyAge && (ChildMaxEmpathyAge == 0 || age <= ChildMaxEmpathyAge);
    }

    private static void AddCountToState(
        ref ThoughtState state,
        int additionalCount,
        int singleStage,
        int multipleStage)
    {
        if (additionalCount == 0 || state.Active && state.StageIndex == multipleStage)
        {
            return;
        }

        if (!state.Active)
        {
            state = ThoughtState.ActiveAtStage(additionalCount == 1 ? singleStage : multipleStage);
            return;
        }

        state = ThoughtState.ActiveAtStage(multipleStage);
    }

    private static ThoughtState SickChildState(int sickCount, int criticalCount)
    {
        if (sickCount == 1)
        {
            return ThoughtState.ActiveAtStage(criticalCount == 1 ? 1 : 0);
        }

        return ThoughtState.ActiveAtStage(criticalCount == sickCount ? 3 : 2);
    }

    private static Pawn ThoughtPawn(object thought)
    {
        return thought == null ? null : thoughtPawnField.GetValue(thought) as Pawn;
    }

    private static void GainMemory(Pawn pawn, ThoughtDef thought, Pawn otherPawn)
    {
        if (thought != null && pawn?.needs?.mood?.thoughts?.memories != null)
        {
            pawn.needs.mood.thoughts.memories.TryGainMemory(thought, otherPawn);
        }
    }

    private static int AgeOfCaring => Setting(ageOfCaringField, 0);

    private static bool ChildrenWorryAboutParents => Setting(childrenWorryAboutParentsField, true);

    private static int ChildMinEmpathyAge => Setting(childMinEmpathyAgeField, 6);

    private static int ChildMaxEmpathyAge => Setting(childMaxEmpathyAgeField, 0);

    private static bool ProudForMasterpiece => Setting(proudForMasterpieceField, true);

    private static bool ProudForSkillUp => Setting(proudForSkillUpField, true);

    private static T Setting<T>(FieldInfo field, T fallback)
    {
        return field.GetValue(settings) is T value ? value : fallback;
    }
}
