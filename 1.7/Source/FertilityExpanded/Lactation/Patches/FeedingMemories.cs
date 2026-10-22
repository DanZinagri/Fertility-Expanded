using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace FertilityExpanded.Patches;

[HarmonyPatch(typeof(MemoryThoughtHandler))]
[HarmonyPatch(nameof(MemoryThoughtHandler.TryGainMemory), new Type[] { typeof(ThoughtDef), typeof(Pawn), typeof(Precept) })]
public static class FeedingMemoriesPatch
{
    public static void Postfix(MemoryThoughtHandler __instance, ThoughtDef def, Pawn otherPawn)
    {
        FeedingBeliefs.HandleSuccessfulFeedingMemory(__instance?.pawn, def, otherPawn);
        if (def == ThoughtDefOf.BreastfedBaby || def == ThoughtDefOf.FedBaby)
        {
            PostpartumCare.NotifyCareCompleted(__instance?.pawn, otherPawn);
        }
        if (ModsConfig.IdeologyActive && (def == ThoughtDefOf.BreastfedBaby || def == ThoughtDefOf.FedBaby))
        {
            CaregiverPreference.NotifyMeaningfulCare(__instance?.pawn, otherPawn);
        }
        else if (ModsConfig.IdeologyActive && def == ThoughtDefOf.PlayedWithMe)
        {
            CaregiverPreference.NotifyMeaningfulCare(otherPawn, __instance?.pawn);
        }
    }
}
