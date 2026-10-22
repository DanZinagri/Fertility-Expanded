using HarmonyLib;
using FertilityExpanded.Patches.Compatibility;
using RimWorld;
using Verse;

namespace FertilityExpanded.Patches;

[HarmonyPatch(typeof(StatPart_FertilityByGenderAge), nameof(StatPart_FertilityByGenderAge.ExplanationPart))]
public static class AgeExplanationPatch
{
    public static bool Prefix(StatRequest req, ref string __result)
    {
        if (req.Thing is Pawn pawn && FertilityAgeCurve.IsCycleOnsetPending(pawn))
        {
            __result = "FE_LabelMultiplier".Translate("StatsReport_FertilityAgeFactor".Translate().Named("LABEL"), 0f.ToStringPercent().Named("VALUE"));
            return false;
        }

        if (!Features.FertilityCycles)
        {
            return true;
        }

        __result = req.Thing is Pawn fertilityPawn
            ? "FE_LabelMultiplier".Translate("StatsReport_FertilityAgeFactor".Translate().Named("LABEL"), FertilityAgeCurve.Factor(fertilityPawn).ToStringPercent().Named("VALUE"))
            : null;
        return false;
    }
}
