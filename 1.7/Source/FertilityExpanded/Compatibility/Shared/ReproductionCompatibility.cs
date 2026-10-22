using RimWorld;
using Verse;

namespace FertilityExpanded.Patches.Compatibility;

internal static class ReproductionCompat
{
    private const string FemaleReproductiveOrganTag = "SEX_FemaleReproductiveOrgan";
    private const string NeuteredTag = "SEX_Neutered";
    private const string SterilizedTag = "Sterilized";
    private const string ReversibleSterilizedTag = "ReversibleSterilized";
    private const string TemporaryContraceptionTag = "FE_TemporaryContraception";

    public static bool CanExperienceFertilityCycle(Pawn pawn)
    {
        return HasHumanlikeFertilityBasis(pawn) && HasGestationalReproductiveBasis(pawn);
    }

    public static bool IsCycleBlockingHediff(HediffDef hediffDef)
    {
        return IsPregnancyOrLaborHediff(hediffDef)
            || IntimacyRecovery.IsPostpartumRecovery(hediffDef)
            || hediffDef == FE_DefOf.FE_Postmenopausal
            || IsContraceptiveHediff(hediffDef);
    }

    public static bool IsMenopauseBlockingHediff(HediffDef hediffDef)
    {
        return IsPregnancyOrLaborHediff(hediffDef)
            || IntimacyRecovery.IsPostpartumRecovery(hediffDef);
    }

    public static bool HasCycleBlockingHediff(Pawn pawn)
    {
        if (pawn?.health?.hediffSet == null)
        {
            return true;
        }

        foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
        {
            if (IsCycleBlockingHediff(hediff.def))
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasMenopauseBlockingHediff(Pawn pawn)
    {
        if (pawn?.health?.hediffSet == null)
        {
            return true;
        }

        foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
        {
            if (IsMenopauseBlockingHediff(hediff.def))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPregnancyOrLaborHediff(HediffDef hediffDef)
    {
        return hediffDef?.pregnant == true
            || hediffDef == HediffDefOf.PregnancyLabor
            || hediffDef == HediffDefOf.PregnancyLaborPushing;
    }

    private static bool IsContraceptiveHediff(HediffDef hediffDef)
    {
        return HasHediffTag(hediffDef, SterilizedTag)
            || HasHediffTag(hediffDef, ReversibleSterilizedTag)
            || HasHediffTag(hediffDef, TemporaryContraceptionTag);
    }

    private static bool HasHediffTag(HediffDef hediffDef, string tag)
    {
        return hediffDef?.tags?.Contains(tag) == true;
    }

    private static bool HasHumanlikeFertilityBasis(Pawn pawn)
    {
        return ModsConfig.BiotechActive
            && pawn != null
            && !pawn.Dead
            && pawn.health != null
            && pawn.RaceProps?.Humanlike == true;
    }

    private static bool HasGestationalReproductiveBasis(Pawn pawn)
    {
        if (!ModDetection.GenderWorksActive)
        {
            return pawn.gender == Gender.Female;
        }

        if (pawn.health.hediffSet == null)
        {
            return false;
        }

        bool hasFemaleOrgan = false;
        for (int i = 0; i < pawn.health.hediffSet.hediffs.Count; i++)
        {
            HediffDef def = pawn.health.hediffSet.hediffs[i]?.def;
            if (def?.tags == null)
            {
                continue;
            }

            if (def.tags.Contains(NeuteredTag))
            {
                return false;
            }

            if (def.tags.Contains(FemaleReproductiveOrganTag))
            {
                hasFemaleOrgan = true;
            }
        }

        return hasFemaleOrgan;
    }
}
