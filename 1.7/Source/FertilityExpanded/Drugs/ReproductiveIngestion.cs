using FertilityExpanded.Patches.Compatibility;
using RimWorld;
using Verse;

namespace FertilityExpanded;

public class ReproductiveIngestion : IngestionOutcomeDoer
{
    public HediffDef hediffDef;
    public bool gestationalOnly;
    public bool removeCycle;
    public float minimumBiologicalAge = 14f;

    protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount)
    {
        if (!RjwDrugs.IsDrugEnabled(ingested?.def))
        {
            if (RjwDrugs.FertilityExpandedDrugsDisabled)
            {
                Log.WarningOnce("[Fertility Expanded] A Fertility Expanded reproductive drug was consumed while RJW owns reproductive drug handling. No fertility hediff was applied.", 108472304);
            }

            return;
        }

        if (pawn?.RaceProps?.Humanlike != true || pawn.health?.hediffSet == null || pawn.ageTracker == null)
        {
            return;
        }

        if (pawn.ageTracker.AgeBiologicalYearsFloat < minimumBiologicalAge)
        {
            return;
        }

        if (gestationalOnly && !ReproductionCompat.CanExperienceFertilityCycle(pawn))
        {
            return;
        }

        if (hediffDef == null)
        {
            Log.WarningOnce("[Fertility Expanded] Reproductive drug ingestion outcome is missing a hediffDef.", 108472302);
            return;
        }

        Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
        if (hediff == null)
        {
            hediff = HediffMaker.MakeHediff(hediffDef, pawn);
            pawn.health.AddHediff(hediff);
        }

        hediff.Severity = ResetSeverity(hediffDef);

        if (removeCycle)
        {
            FertilityCycles.RemoveCycle(pawn);
        }
    }

    private static float ResetSeverity(HediffDef def)
    {
        if (def.initialSeverity > 0f)
        {
            return def.initialSeverity;
        }

        return def.maxSeverity > 0f ? def.maxSeverity : 1f;
    }
}
