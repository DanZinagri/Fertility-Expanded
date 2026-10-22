using HarmonyLib;
using FertilityExpanded.Patches.Compatibility;
using RimWorld;
using Verse;

namespace FertilityExpanded.Patches;

internal static class FertilityAgeCurve
{
    private static readonly SimpleCurve Male = new SimpleCurve
    {
        new CurvePoint(14f, 0f),
        new CurvePoint(18f, 1f),
        new CurvePoint(50f, 1f),
        new CurvePoint(90f, 0f)
    };

    private static readonly SimpleCurve Female = new SimpleCurve
    {
        new CurvePoint(FertilityCycles.MinimumCycleAge, 1f),
        new CurvePoint(28f, 1f),
        new CurvePoint(35f, 0.80f),
        new CurvePoint(40f, 0.45f),
        new CurvePoint(45f, 0.05f),
        new CurvePoint(50f, 0f)
    };

    public static bool IsCycleOnsetPending(Pawn pawn)
    {
        return Features.FertilityCycles
            && ReproductionCompat.CanExperienceFertilityCycle(pawn)
            && !FertilityCycles.HasDevelopedCycle(pawn);
    }

    public static float Factor(Pawn pawn)
    {
        if (!ReproductionCompat.CanExperienceFertilityCycle(pawn))
        {
            return Male.Evaluate(pawn.ageTracker.AgeBiologicalYearsFloat);
        }

        float age = pawn.ageTracker.AgeBiologicalYearsFloat;
        return age < FertilityCycles.MinimumCycleAge ? 0f : Female.Evaluate(age);
    }
}
