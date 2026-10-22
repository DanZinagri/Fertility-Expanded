using System.Collections.Generic;
using FertilityExpanded.Patches.Compatibility;
using RimWorld;
using Verse;

namespace FertilityExpanded;

public static class Menopause
{
    private const int FirstMenopauseRollAge = 45;

    private static float MenopauseChanceAtAge(int age)
    {
        return age switch
        {
            < 45 => 0f,
            45 => 0.01f,
            46 => 0.01f,
            47 => 0.02f,
            48 => 0.04f,
            49 => 0.07f,
            50 => 0.12f,
            51 => 0.18f,
            52 => 0.25f,
            53 => 0.35f,
            54 => 0.45f,
            55 => 0.60f,
            56 => 0.75f,
            57 => 0.90f,
            _ => 1f
        };
    }

    internal static MenopauseCheckResult CheckAtBirthday(
        Pawn pawn,
        int birthdayAge,
        Dictionary<int, int> lastRollAges)
    {
        return CheckThroughAge(pawn, birthdayAge, lastRollAges);
    }

    internal static MenopauseCheckResult Reconcile(
        Pawn pawn,
        Dictionary<int, int> lastRollAges)
    {
        int age = pawn?.ageTracker?.AgeBiologicalYears ?? -1;
        return CheckThroughAge(pawn, age, lastRollAges);
    }

    private static MenopauseCheckResult CheckThroughAge(
        Pawn pawn,
        int age,
        Dictionary<int, int> lastRollAges)
    {
        if (!Features.Menopause
            || lastRollAges == null
            || !CanEverHaveMenopause(pawn)
            || age < FirstMenopauseRollAge)
        {
            return MenopauseCheckResult.NotNeeded;
        }

        HediffSet hediffSet = pawn.health.hediffSet;
        if (hediffSet.HasHediff(FE_DefOf.FE_Postmenopausal)
            || hediffSet.HasHediff(FE_DefOf.FE_MenopauseTransition))
        {
            return MenopauseCheckResult.NotNeeded;
        }

        if (ReproductionCompat.HasMenopauseBlockingHediff(pawn))
        {
            return MenopauseCheckResult.Deferred;
        }

        int key = pawn.thingIDNumber;
        bool hasPreviousRoll = lastRollAges.TryGetValue(key, out int lastRolledAge);
        if (hasPreviousRoll && lastRolledAge >= age)
        {
            return MenopauseCheckResult.Processed;
        }

        int firstAgeToRoll = hasPreviousRoll && lastRolledAge >= FirstMenopauseRollAge
            ? lastRolledAge + 1
            : FirstMenopauseRollAge;
        int transitionStartAge = -1;

        for (int rollAge = firstAgeToRoll; rollAge <= age; rollAge++)
        {
            float chance = MenopauseChanceAtAge(rollAge);
            if (chance >= 1f || Rand.Chance(chance))
            {
                transitionStartAge = rollAge;
                break;
            }
        }

        lastRollAges[key] = age;
        if (transitionStartAge >= 0)
        {
            StartTransition(pawn);
        }

        return MenopauseCheckResult.Processed;
    }

    public static bool IsTransitionActive(Pawn pawn)
    {
        return pawn?.health?.hediffSet?.HasHediff(FE_DefOf.FE_MenopauseTransition) == true;
    }

    public static void RemoveTransition(Pawn pawn)
    {
        RemoveHediff(pawn, FE_DefOf.FE_MenopauseTransition);
    }

    public static void CompleteMenopause(Pawn pawn)
    {
        if (!CanEverHaveMenopause(pawn))
        {
            return;
        }

        RemoveTransition(pawn);
        FertilityCycles.RemoveCycle(pawn);

        if (!pawn.health.hediffSet.HasHediff(FE_DefOf.FE_Postmenopausal))
        {
            pawn.health.AddHediff(FE_DefOf.FE_Postmenopausal);
        }
    }

    public static void StartTransition(Pawn pawn)
    {
        if (!Features.Menopause || !CanCheckMenopause(pawn))
        {
            return;
        }

        FertilityCycles.EnsureCycle(pawn);

        if (!pawn.health.hediffSet.HasHediff(FE_DefOf.FE_MenopauseTransition))
        {
            pawn.health.AddHediff(FE_DefOf.FE_MenopauseTransition);
        }
    }

    private static bool CanCheckMenopause(Pawn pawn)
    {
        if (!CanEverHaveMenopause(pawn))
        {
            return false;
        }

        HediffSet hediffSet = pawn.health.hediffSet;
        return !hediffSet.HasHediff(FE_DefOf.FE_Postmenopausal)
            && !hediffSet.HasHediff(FE_DefOf.FE_MenopauseTransition)
            && !ReproductionCompat.HasMenopauseBlockingHediff(pawn);
    }

    private static bool CanEverHaveMenopause(Pawn pawn)
    {
        return ReproductionCompat.CanExperienceFertilityCycle(pawn);
    }

    private static void RemoveHediff(Pawn pawn, HediffDef hediffDef)
    {
        Hediff hediff = pawn?.health?.hediffSet?.GetFirstHediffOfDef(hediffDef);
        if (hediff != null)
        {
            pawn.health.RemoveHediff(hediff);
        }
    }
}

internal enum MenopauseCheckResult
{
    NotNeeded,
    Deferred,
    Processed
}
