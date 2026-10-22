using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace FertilityExpanded;

[HarmonyPatch(typeof(ChildcareUtility), nameof(ChildcareUtility.CanAutoBreastfeed))]
public static class NursingEligibilityPatch
{
    public static void Postfix(Pawn mom, Pawn baby, bool forced, ref bool __result, ref ChildcareUtility.BreastfeedFailReason? reason)
    {
        if (__result && !forced && (!CaregiverPreference.AllowsAutomaticFeeding(mom, baby)
            || CaregiverPreference.ShouldDeferAutomaticFeeding(mom, baby, true)))
        {
            __result = false;
            reason = ChildcareUtility.BreastfeedFailReason.MomNotAssignedToBaby;
        }
    }
}
