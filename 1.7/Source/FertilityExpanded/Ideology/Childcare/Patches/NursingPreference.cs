using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace FertilityExpanded;

[HarmonyPatch(typeof(WorkGiver_Breastfeed), nameof(WorkGiver_Breastfeed.JobOnThing))]
[HarmonyPatch(new System.Type[] { typeof(Pawn), typeof(Thing), typeof(bool) })]
public static class NursingPreferencePatch
{
    public static void Postfix(WorkGiver_Breastfeed __instance, Pawn pawn, Thing t, bool forced, ref Job __result)
    {
        if (__result != null && CaregiverPreference.ShouldDefer(pawn, t as Pawn, __instance, forced))
        {
            __result = null;
        }
    }
}
