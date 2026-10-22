using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace FertilityExpanded.Patches;

[HarmonyPatch(typeof(WorkGiver_BottleFeedBaby), nameof(WorkGiver_BottleFeedBaby.JobOnThing))]
public static class BottleFoodPatch
{
    public static void Postfix(Pawn pawn, Thing t, ref Job __result)
    {
        if (__result == null)
        {
            return;
        }

        Pawn baby = t as Pawn;
        Thing foodSource = __result.targetB.Thing;
        if (!FeedingBeliefs.ShouldBlockArtificialFeeding(pawn, baby, foodSource))
        {
            return;
        }

        Thing humanMilk = FeedingBeliefs.FindHumanMilkForBaby(pawn, baby);
        if (humanMilk != null)
        {
            __result = ChildcareUtility.MakeBottlefeedJob(baby, humanMilk);
            return;
        }

        JobFailReason.Is("FE_BreastfeedingRequired".Translate());
        __result = null;
    }
}
