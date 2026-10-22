using HarmonyLib;
using Verse;

namespace FertilityExpanded.Patches;

[HarmonyPatch(typeof(Pawn), nameof(Pawn.SpawnSetup))]
public static class SpawnReconciliationPatch
{
    public static void Postfix(Pawn __instance, bool respawningAfterLoad)
    {
        FertilityCycles.EnsureCycle(__instance);
        if (!respawningAfterLoad)
        {
            PostpartumCare.RegisterRecovery(__instance);
            Current.Game?.GetComponent<GameState>()?.NotifyPawnSpawned(__instance);
        }
    }
}
