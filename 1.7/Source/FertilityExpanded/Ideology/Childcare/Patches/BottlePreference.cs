using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace FertilityExpanded;

[HarmonyPatch(typeof(WorkGiver_BottleFeedBaby), nameof(WorkGiver_BottleFeedBaby.JobOnThing))]
public static class BottlePreferencePatch
{
    public static void Postfix(WorkGiver_BottleFeedBaby __instance, Pawn pawn, Thing t, bool forced, ref Job __result)
    {
        if (__result != null && CaregiverPreference.ShouldDefer(pawn, t as Pawn, __instance, forced))
        {
            __result = null;
        }
    }
}
