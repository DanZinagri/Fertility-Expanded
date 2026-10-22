using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace FertilityExpanded;

[HarmonyPatch(typeof(ChildcareUtility), nameof(ChildcareUtility.FindAutofeedBaby))]
public static class AutofeedSelectionPatch
{
    public static void Postfix(Pawn mom, AutofeedMode priorityLevel, ref Thing food, ref Pawn __result)
    {
        if (__result == null || (CaregiverPreference.AllowsAutomaticFeeding(mom, __result)
            && !CaregiverPreference.ShouldDeferAutomaticFeeding(mom, __result, food == mom)))
        {
            return;
        }

        __result = null;
        food = null;
        bool canBreastfeed = ChildcareUtility.CanBreastfeedNow(mom, out _);
        foreach (Pawn baby in mom.MapHeld.mapPawns.FreeHumanlikesOfFaction(mom.Faction))
        {
            if (!baby.Suspended && CaregiverPreference.AllowsAutomaticFeeding(mom, baby)
                && !CaregiverPreference.ShouldDeferAutomaticFeeding(mom, baby, canBreastfeed)
                && ChildcareUtility.WantsSuckle(baby, out _)
                && baby.mindState.AutofeedSetting(mom) == priorityLevel
                && ChildcareUtility.CanFeedBaby(mom, baby, out _)
                && ChildcareUtility.CanHaulBabyToMomNow(mom, mom, baby, false, out _))
            {
                food = canBreastfeed ? mom : ChildcareUtility.FindBabyFoodForBaby(mom, baby);
                __result = baby;
                return;
            }
        }
    }
}
