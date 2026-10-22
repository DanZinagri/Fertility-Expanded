using HarmonyLib;
using Verse;

namespace FertilityExpanded.Patches;

[HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.AddHediff), typeof(Hediff), typeof(BodyPartRecord), typeof(DamageInfo?), typeof(DamageWorker.DamageResult))]
public static class CycleSuppressionPatch
{
    public static void Postfix(Hediff hediff)
    {
        FertilityCycles.RemoveCycleIfBlockingHediffAdded(hediff?.pawn, hediff?.def);
        PostpartumCare.RegisterRecovery(hediff?.pawn, hediff?.def);
    }
}
