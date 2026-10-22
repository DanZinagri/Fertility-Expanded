using System.Collections;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace FertilityExpanded.Patches.Compatibility;

internal static class FluffyTreeCompat
{
    private const string FamilyMemberTypeName = "FamilyTree.FamilyTreeMap.FamilyMember";

    private static FieldInfo familyMemberPawnField;
    private static FieldInfo familyMemberParentsField;
    private static FieldInfo familyMemberChildrenField;

    private static bool CanUseFamilyRelations => ModDetection.FluffyFamilyTreeActive
        && FE_DefOf.FE_AdoptiveParent != null
        && FE_DefOf.FE_AdoptiveChild != null
        && FE_DefOf.FE_FosterParent != null
        && FE_DefOf.FE_FosterChild != null;

    public static bool Initialize()
    {
        System.Type familyMemberType = AccessTools.TypeByName(FamilyMemberTypeName);
        if (familyMemberType == null)
        {
            Log.Warning("[Fertility Expanded] Could not find " + FamilyMemberTypeName + " for Fluffy Family Tree compatibility.");
            return false;
        }

        familyMemberPawnField = AccessTools.Field(familyMemberType, "Pawn");
        familyMemberParentsField = AccessTools.Field(familyMemberType, "parents");
        familyMemberChildrenField = AccessTools.Field(familyMemberType, "children");
        if (familyMemberPawnField == null || familyMemberParentsField == null || familyMemberChildrenField == null)
        {
            Log.Warning("[Fertility Expanded] Could not find the expected FamilyMember fields for Fluffy Family Tree compatibility.");
            return false;
        }

        return true;
    }

    public static void VisibleInTabPostfix(PawnRelationDef relation, ref bool __result)
    {
        if (!__result && CanUseFamilyRelations && IsFertilityExpandedFamilyRelation(relation))
        {
            __result = true;
        }
    }

    public static void CreateParentRelationshipsPostfix(object __instance, object[] __args)
    {
        if (!CanUseFamilyRelations
            || __instance == null
            || __args == null
            || __args.Length != 1
            || __args[0] is not IEnumerable allNodes)
        {
            return;
        }

        Pawn parent = familyMemberPawnField.GetValue(__instance) as Pawn;
        IList children = familyMemberChildrenField.GetValue(__instance) as IList;
        if (parent == null || children == null)
        {
            return;
        }

        foreach (object childNode in allNodes)
        {
            if (childNode == null)
            {
                continue;
            }

            Pawn child = familyMemberPawnField.GetValue(childNode) as Pawn;
            if (!HasFertilityExpandedParentRelation(child, parent))
            {
                continue;
            }

            IList parents = familyMemberParentsField.GetValue(childNode) as IList;
            if (parents == null)
            {
                continue;
            }

            AddDistinct(parents, __instance);
            AddDistinct(children, childNode);
        }
    }

    private static bool HasFertilityExpandedParentRelation(Pawn child, Pawn parent)
    {
        if (child?.relations == null || parent == null)
        {
            return false;
        }

        foreach (DirectPawnRelation relation in child.relations.DirectRelations)
        {
            if (relation.otherPawn == parent && IsFertilityExpandedParentRelation(relation.def))
            {
                return true;
            }
        }

        return false;
    }

    private static void AddDistinct(IList list, object value)
    {
        if (!list.Contains(value))
        {
            list.Add(value);
        }
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
}
