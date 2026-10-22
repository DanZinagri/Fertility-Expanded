using HarmonyLib;
using Verse;

namespace FertilityExpanded.Patches;

[HarmonyPatch(typeof(Hediff_Labor), nameof(Hediff_Labor.PreRemoved))]
public static class LaborCompletionPatch
{
    public static void Postfix(Hediff_Labor __instance)
    {
        MultipleBirths.CopyLaborToLaborPushing(__instance);
    }
}
