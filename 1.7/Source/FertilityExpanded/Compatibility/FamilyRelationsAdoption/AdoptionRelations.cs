using RimWorld;
using Verse;

namespace FertilityExpanded.Patches.Compatibility;

internal static class AdoptionRelationsCompat
{
    private const string AdoptiveParentDefName = "FRA_AdoptiveParent";
    private const string AdoptedChildDefName = "FRA_AdoptedChild";

    private static PawnRelationDef adoptiveParent;
    private static PawnRelationDef adoptedChild;

    public static void Initialize()
    {
        if (!ModDetection.FamilyRelationsAdoptionActive)
        {
            return;
        }

        adoptiveParent = DefDatabase<PawnRelationDef>.GetNamedSilentFail(AdoptiveParentDefName);
        adoptedChild = DefDatabase<PawnRelationDef>.GetNamedSilentFail(AdoptedChildDefName);
        if (adoptiveParent == null || adoptedChild == null)
        {
            Log.Warning("[Fertility Expanded] Family Relations: Adoption is active, but its adoptive parent or adopted child relation was not found.");
        }
    }

    public static bool IsAdoptiveParentRelation(PawnRelationDef relationDef)
    {
        return relationDef != null && relationDef == adoptiveParent;
    }

    public static bool IsAdoptiveChildRelation(PawnRelationDef relationDef)
    {
        return relationDef != null && relationDef == adoptedChild;
    }

    public static PawnRelationDef AdoptiveParentDef => adoptiveParent;

    public static PawnRelationDef AdoptedChildDef => adoptedChild;
}
