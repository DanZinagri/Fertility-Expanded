using HarmonyLib;
using FertilityExpanded.Patches.Compatibility;
using RimWorld;
using Verse;

namespace FertilityExpanded.Patches;

[HarmonyPatch(typeof(StatPart_FertilityByGenderAge), nameof(StatPart_FertilityByGenderAge.TransformValue))]
public static class AgeFactorPatch
{
    public static bool Prefix(StatRequest req, ref float val)
    {
        if (req.Thing is Pawn pawn && FertilityAgeCurve.IsCycleOnsetPending(pawn))
        {
            val = 0f;
            return false;
        }

        if (!Features.FertilityCycles)
        {
            return true;
        }

        if (req.Thing is Pawn fertilityPawn)
        {
            val *= FertilityAgeCurve.Factor(fertilityPawn);
        }

        return false;
    }
}
