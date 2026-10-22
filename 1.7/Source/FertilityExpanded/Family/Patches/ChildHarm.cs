using HarmonyLib;
using Verse;

namespace FertilityExpanded.Patches;

[HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.PostApplyDamage))]
public static class ChildHarmPatch
{
    public static void Postfix(Pawn_HealthTracker __instance, DamageInfo dinfo, float totalDamageDealt)
    {
        ChildHarm.NotifyChildDamaged(__instance?.hediffSet?.pawn, dinfo, totalDamageDealt);
    }
}
