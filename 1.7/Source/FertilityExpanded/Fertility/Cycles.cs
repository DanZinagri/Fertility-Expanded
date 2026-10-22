using RimWorld;
using FertilityExpanded.Patches.Compatibility;
using Verse;

namespace FertilityExpanded;

public static class FertilityCycles
{
    internal const int MinimumCycleAge = 14;
    internal const int GuaranteedCycleAge = 18;

    public static void EnsureCycle(Pawn pawn)
    {
        if (!Features.FertilityCycles)
        {
            RemoveCycle(pawn);
            Menopause.RemoveTransition(pawn);
            return;
        }

        if (!ReproductionCompat.CanExperienceFertilityCycle(pawn)
            || pawn.ageTracker.AgeBiologicalYears < MinimumCycleAge)
        {
            RemoveCycle(pawn);
            return;
        }

        GameState component = Current.Game?.GetComponent<GameState>();
        component?.RecordExistingFertilityCycle(pawn);
        if (!HasDevelopedCycle(pawn) && component?.TryDevelopFertilityCycle(pawn) != true)
        {
            RemoveCycle(pawn);
            return;
        }

        if (ReproductionCompat.HasCycleBlockingHediff(pawn))
        {
            RemoveCycle(pawn);
            return;
        }

        if (!pawn.health.hediffSet.HasHediff(FE_DefOf.FE_FertilityCycle))
        {
            pawn.health.AddHediff(FE_DefOf.FE_FertilityCycle);
        }
    }

    internal static bool HasDevelopedCycle(Pawn pawn)
    {
        if (pawn?.ageTracker == null || pawn.ageTracker.AgeBiologicalYears < MinimumCycleAge)
        {
            return false;
        }

        if (pawn.ageTracker.AgeBiologicalYears >= GuaranteedCycleAge
            || pawn.health?.hediffSet?.HasHediff(FE_DefOf.FE_FertilityCycle) == true)
        {
            return true;
        }

        return Current.Game?.GetComponent<GameState>()?.HasDevelopedFertilityCycle(pawn) == true;
    }

    internal static float CycleOnsetChanceAtAge(int age)
    {
        return age switch
        {
            14 => 0.20f,
            15 => 0.40f,
            16 => 0.60f,
            17 => 0.80f,
            >= GuaranteedCycleAge => 1f,
            _ => 0f
        };
    }

    public static void AddPostpartumRecovery(Pawn pawn)
    {
        if (!Features.PostpartumRecovery || pawn == null || pawn.health == null)
        {
            return;
        }

        RemoveCycle(pawn);

        Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(FE_DefOf.FE_PostpartumRecovery);
        if (existing != null)
        {
            pawn.health.RemoveHediff(existing);
        }

        pawn.health.AddHediff(FE_DefOf.FE_PostpartumRecovery);
    }

    public static void RemoveCycleIfBlockingHediffAdded(Pawn pawn, HediffDef addedDef)
    {
        if (pawn == null || addedDef == null)
        {
            return;
        }

        if (ReproductionCompat.IsCycleBlockingHediff(addedDef))
        {
            RemoveCycle(pawn);
        }

        if (ReproductionCompat.IsMenopauseBlockingHediff(addedDef))
        {
            Menopause.RemoveTransition(pawn);
        }
    }

    public static void RemoveCycle(Pawn pawn)
    {
        Hediff cycle = pawn?.health?.hediffSet?.GetFirstHediffOfDef(FE_DefOf.FE_FertilityCycle);
        if (cycle != null)
        {
            pawn.health.RemoveHediff(cycle);
        }
    }
}
