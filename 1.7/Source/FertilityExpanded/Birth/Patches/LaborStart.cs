using HarmonyLib;
using Verse;

namespace FertilityExpanded.Patches;

[HarmonyPatch(typeof(Hediff_Pregnant), nameof(Hediff_Pregnant.StartLabor))]
public static class LaborStartPatch
{
    public static void Postfix(Hediff_Pregnant __instance)
    {
        MultipleBirths.CopyPregnancyToLabor(__instance);
    }
}
