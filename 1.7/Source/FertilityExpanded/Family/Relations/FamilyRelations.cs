using FertilityExpanded.Patches.Compatibility;
using RimWorld;
using Verse;

namespace FertilityExpanded;

internal static class FamilyRelations
{
    public static bool IsAdoptiveParentRelation(PawnRelationDef relationDef)
    {
        return relationDef != null
            && (relationDef == FE_DefOf.FE_AdoptiveParent
                || AdoptionRelationsCompat.IsAdoptiveParentRelation(relationDef));
    }

    public static bool IsAdoptiveChildRelation(PawnRelationDef relationDef)
    {
        return relationDef != null
            && (relationDef == FE_DefOf.FE_AdoptiveChild
                || AdoptionRelationsCompat.IsAdoptiveChildRelation(relationDef));
    }

    public static bool IsFosterParentRelation(PawnRelationDef relationDef)
    {
        return relationDef != null && relationDef == FE_DefOf.FE_FosterParent;
    }

    public static bool IsFosterChildRelation(PawnRelationDef relationDef)
    {
        return relationDef != null && relationDef == FE_DefOf.FE_FosterChild;
    }

    public static bool IsNonVanillaParentRelation(PawnRelationDef relationDef)
    {
        return IsAdoptiveParentRelation(relationDef) || IsFosterParentRelation(relationDef);
    }

    public static bool IsParentLikeRelation(PawnRelationDef relationDef)
    {
        return relationDef != null
            && (relationDef == PawnRelationDefOf.Parent || IsNonVanillaParentRelation(relationDef));
    }

    public static bool HasParentLikeRelation(Pawn child, Pawn parent)
    {
        return HasRelation(child, parent, IsParentLikeRelation);
    }

    public static bool HasAdoptiveParentRelation(Pawn child, Pawn parent)
    {
        return HasRelation(child, parent, IsAdoptiveParentRelation);
    }

    public static bool HasAnyAdoptiveParentRelation(Pawn child)
    {
        return HasRelation(child, null, IsAdoptiveParentRelation);
    }

    public static bool HasLivingAdoptiveParentRelation(Pawn child)
    {
        if (child?.relations == null)
        {
            return false;
        }

        foreach (DirectPawnRelation relation in child.relations.DirectRelations)
        {
            Pawn parent = relation.otherPawn;
            if (IsAdoptiveParentRelation(relation.def)
                && parent != null
                && !parent.Destroyed
                && !parent.Dead)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasRelation(Pawn child, Pawn parent, System.Predicate<PawnRelationDef> predicate)
    {
        if (child?.relations == null || predicate == null)
        {
            return false;
        }

        foreach (DirectPawnRelation relation in child.relations.DirectRelations)
        {
            if ((parent == null || relation.otherPawn == parent) && predicate(relation.def))
            {
                return true;
            }
        }

        return false;
    }
}
