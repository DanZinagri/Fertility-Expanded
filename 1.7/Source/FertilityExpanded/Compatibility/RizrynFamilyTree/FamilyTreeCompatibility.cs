using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace FertilityExpanded.Patches.Compatibility;

internal static class RizrynTreeCompat
{
    private static bool addingFamilyGroups;
    private static MethodInfo buildParentChildFamiliesMethod;
    private static FieldInfo extendedRelationRelativeField;
    private static FieldInfo extendedRelationNameField;

    private static bool CanUseFamilyTreeRelations => ModDetection.RizrynFamilyTreeActive
        && FE_DefOf.FE_AdoptiveParent != null
        && FE_DefOf.FE_AdoptiveChild != null
        && FE_DefOf.FE_FosterParent != null
        && FE_DefOf.FE_FosterChild != null;

    public static void IsActive_Postfix(ref bool __result)
    {
        if (CanUseFamilyTreeRelations)
        {
            __result = true;
        }
    }

    public static void IsAdoptiveRelationDef_Postfix(PawnRelationDef def, ref bool __result)
    {
        if (!__result && IsFertilityExpandedFamilyRelation(def))
        {
            __result = true;
        }
    }

    public static void GetAdoptiveParents_Postfix(Pawn pawn, ref IEnumerable<Pawn> __result)
    {
        if (!CanUseFamilyTreeRelations)
        {
            return;
        }

        __result = AppendDistinct(__result, FertilityExpandedParentsOf(pawn));
    }

    public static void GetAdoptiveChildren_Postfix(Pawn pawn, ref IEnumerable<Pawn> __result)
    {
        if (!CanUseFamilyTreeRelations)
        {
            return;
        }

        __result = AppendDistinct(__result, FertilityExpandedChildrenOf(pawn));
    }

    public static void HasAnyAdoptiveFamily_Postfix(Pawn pawn, ref bool __result)
    {
        if (__result || !CanUseFamilyTreeRelations || pawn?.relations == null)
        {
            return;
        }

        foreach (DirectPawnRelation relation in pawn.relations.DirectRelations)
        {
            if (IsFertilityExpandedFamilyRelation(relation.def))
            {
                __result = true;
                return;
            }
        }

        foreach (Pawn ignored in FertilityExpandedChildrenOf(pawn))
        {
            __result = true;
            return;
        }
    }

    public static void GetAdoptiveAncestors_Postfix(Pawn pawn, Dictionary<Pawn, int> __result)
    {
        if (!CanUseFamilyTreeRelations || pawn == null || __result == null)
        {
            return;
        }

        Queue<Pair<Pawn, int>> queue = new Queue<Pair<Pawn, int>>();
        queue.Enqueue(new Pair<Pawn, int>(pawn, 0));

        while (queue.Count > 0)
        {
            Pair<Pawn, int> current = queue.Dequeue();
            Pawn currentPawn = current.First;
            int currentDistance = current.Second;

            if (!__result.ContainsKey(currentPawn) || currentDistance < __result[currentPawn])
            {
                __result[currentPawn] = currentDistance;
            }

            foreach (Pawn parent in FertilityExpandedParentsOf(currentPawn))
            {
                int parentDistance = currentDistance + 1;
                if (!__result.ContainsKey(parent) || parentDistance < __result[parent])
                {
                    queue.Enqueue(new Pair<Pawn, int>(parent, parentDistance));
                }
            }
        }
    }

    public static void CalculateAllExtendedAdoptiveRelations_Postfix(Pawn subject, object __result)
    {
        if (!CanUseFamilyTreeRelations || subject == null || __result is not IEnumerable relations)
        {
            return;
        }

        foreach (object relation in relations)
        {
            if (relation == null)
            {
                continue;
            }

            EnsureExtendedRelationFields(relation.GetType());
            Pawn relative = extendedRelationRelativeField?.GetValue(relation) as Pawn;
            string directLabel = DirectFertilityExpandedRelationLabel(subject, relative);
            if (!directLabel.NullOrEmpty())
            {
                extendedRelationNameField?.SetValue(relation, directLabel);
            }
        }
    }

    public static void BuildParentChildFamilies_Postfix(object __instance, PawnRelationDef parentDef)
    {
        if (!CanUseFamilyTreeRelations || addingFamilyGroups || parentDef != PawnRelationDefOf.Parent)
        {
            return;
        }

        MethodInfo method = BuildParentChildFamiliesMethod;
        if (method == null)
        {
            return;
        }

        addingFamilyGroups = true;
        try
        {
            method.Invoke(__instance, new object[] { FE_DefOf.FE_AdoptiveParent, true });
            method.Invoke(__instance, new object[] { FE_DefOf.FE_FosterParent, true });
            if (AdoptionRelationsCompat.AdoptiveParentDef != null)
            {
                method.Invoke(__instance, new object[] { AdoptionRelationsCompat.AdoptiveParentDef, true });
            }
        }
        finally
        {
            addingFamilyGroups = false;
        }
    }

    private static IEnumerable<Pawn> AppendDistinct(IEnumerable<Pawn> original, IEnumerable<Pawn> additional)
    {
        HashSet<Pawn> seen = new HashSet<Pawn>();

        if (original != null)
        {
            foreach (Pawn pawn in original)
            {
                if (pawn != null && seen.Add(pawn))
                {
                    yield return pawn;
                }
            }
        }

        if (additional != null)
        {
            foreach (Pawn pawn in additional)
            {
                if (pawn != null && seen.Add(pawn))
                {
                    yield return pawn;
                }
            }
        }
    }

    private static IEnumerable<Pawn> FertilityExpandedParentsOf(Pawn pawn)
    {
        if (pawn?.relations == null)
        {
            yield break;
        }

        foreach (DirectPawnRelation relation in pawn.relations.DirectRelations)
        {
            if (IsFertilityExpandedParentRelation(relation.def) && relation.otherPawn != null)
            {
                yield return relation.otherPawn;
            }
        }
    }

    private static IEnumerable<Pawn> FertilityExpandedChildrenOf(Pawn parent)
    {
        if (parent?.relations == null)
        {
            yield break;
        }

        HashSet<Pawn> seen = new HashSet<Pawn>();
        foreach (Pawn candidate in parent.relations.PotentiallyRelatedPawns)
        {
            if (candidate?.relations == null || !seen.Add(candidate))
            {
                continue;
            }

            foreach (DirectPawnRelation relation in candidate.relations.DirectRelations)
            {
                if (relation.otherPawn == parent && IsFertilityExpandedParentRelation(relation.def))
                {
                    yield return candidate;
                    break;
                }
            }
        }
    }

    private static string DirectFertilityExpandedRelationLabel(Pawn subject, Pawn relative)
    {
        if (subject?.relations == null || relative == null)
        {
            return null;
        }

        foreach (DirectPawnRelation relation in subject.relations.DirectRelations)
        {
            if (relation.otherPawn == relative && IsFertilityExpandedParentRelation(relation.def))
            {
                return relation.def.GetGenderSpecificLabelCap(relative);
            }
        }

        if (relative.relations == null)
        {
            return null;
        }

        foreach (DirectPawnRelation relation in relative.relations.DirectRelations)
        {
            if (relation.otherPawn != subject || !IsFertilityExpandedParentRelation(relation.def))
            {
                continue;
            }

            PawnRelationDef childRelation = ChildRelationFor(relation.def);
            return childRelation?.GetGenderSpecificLabelCap(relative);
        }

        return null;
    }

    private static bool IsFertilityExpandedFamilyRelation(PawnRelationDef relationDef)
    {
        return FamilyRelations.IsAdoptiveParentRelation(relationDef)
            || FamilyRelations.IsAdoptiveChildRelation(relationDef)
            || FamilyRelations.IsFosterParentRelation(relationDef)
            || FamilyRelations.IsFosterChildRelation(relationDef);
    }

    private static bool IsFertilityExpandedParentRelation(PawnRelationDef relationDef)
    {
        return FamilyRelations.IsNonVanillaParentRelation(relationDef);
    }

    private static PawnRelationDef ChildRelationFor(PawnRelationDef parentRelation)
    {
        if (parentRelation == FE_DefOf.FE_AdoptiveParent)
        {
            return FE_DefOf.FE_AdoptiveChild;
        }

        if (parentRelation == FE_DefOf.FE_FosterParent)
        {
            return FE_DefOf.FE_FosterChild;
        }

        if (AdoptionRelationsCompat.IsAdoptiveParentRelation(parentRelation))
        {
            return AdoptionRelationsCompat.AdoptedChildDef;
        }

        return null;
    }

    private static void EnsureExtendedRelationFields(System.Type relationType)
    {
        if (extendedRelationRelativeField != null && extendedRelationNameField != null)
        {
            return;
        }

        extendedRelationRelativeField = AccessTools.Field(relationType, "Relative");
        extendedRelationNameField = AccessTools.Field(relationType, "RelationshipName");
    }

    private static MethodInfo BuildParentChildFamiliesMethod
    {
        get
        {
            if (buildParentChildFamiliesMethod == null)
            {
                buildParentChildFamiliesMethod = AccessTools.Method(
                    "FamilyTreeMod.MainTabWindow_FamilyTree:BuildParentChildFamilies",
                    new[] { typeof(PawnRelationDef), typeof(bool) });
            }

            return buildParentChildFamiliesMethod;
        }
    }
}
