using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace FertilityExpanded;

[HarmonyPatch(typeof(WorkGiver_PlayWithBaby), nameof(WorkGiver_PlayWithBaby.JobOnThing))]
public static class PlayPreferencePatch
{
    public static void Postfix(WorkGiver_PlayWithBaby __instance, Pawn pawn, Thing t, bool forced, ref Job __result)
    {
        if (__result != null && CaregiverPreference.ShouldDefer(pawn, t as Pawn, __instance, forced))
        {
            __result = null;
        }
    }
}
