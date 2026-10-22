using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace FertilityExpanded;

[HarmonyPatch(typeof(ChildcareUtility), nameof(ChildcareUtility.CanBreastfeedMothers))]
public static class NurseOrderPatch
{
    public static void Postfix(Pawn baby, bool includeAutoBreastfeedOff, ref IEnumerable<Pawn> __result)
    {
        if (!ModsConfig.IdeologyActive)
        {
            return;
        }

        CaregiverPreference.CarePolicy policy = CaregiverPreference.PolicyFor(baby);
        if (policy == CaregiverPreference.CarePolicy.Supported || policy == CaregiverPreference.CarePolicy.FamilyLed)
        {
            __result = CaregiverPreference.OrderBreastfeeders(__result, baby, policy, includeAutoBreastfeedOff);
        }
    }
}
