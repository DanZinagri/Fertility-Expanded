using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FertilityExpanded;

public static class MultipleBirths
{
    public static void CopyPregnancyToLabor(Hediff_Pregnant pregnancy)
    {
        if (pregnancy?.pawn?.health == null)
        {
            return;
        }

        Hediff labor = pregnancy.pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PregnancyLabor);
        CopyMultipleBirthComp(pregnancy, labor);
    }

    public static void CopyLaborToLaborPushing(Hediff_Labor labor)
    {
        if (labor?.pawn?.health == null)
        {
            return;
        }

        Hediff laborPushing = labor.pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PregnancyLaborPushing);
        CopyMultipleBirthComp(labor, laborPushing);
    }

    public static int ConsumeExtraBirths(Pawn birther)
    {
        if (birther?.health == null)
        {
            return 0;
        }

        Hediff laborPushing = birther.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PregnancyLaborPushing);
        MultipleBirthComp comp = laborPushing?.TryGetComp<MultipleBirthComp>();
        return comp?.ConsumeExtraBirths() ?? 0;
    }

    public static bool TrySetPregnancyBabyCountForDebug(Pawn pawn, int babyCount)
    {
        MultipleBirthComp comp = FindActiveMultipleBirthComp(pawn);
        if (comp == null)
        {
            return false;
        }

        comp.SetBabyCountForDebug(babyCount);
        return true;
    }

    public static void RemoveDuplicateBirthRecoveryHediffs(Pawn birther)
    {
        RemoveDuplicateHediffsOfDef(birther, HediffDefOf.PostpartumExhaustion);
        RemoveDuplicateHediffsOfDef(birther, HediffDefOf.Lactating);
    }

    private static MultipleBirthComp FindActiveMultipleBirthComp(Pawn pawn)
    {
        if (pawn?.health?.hediffSet == null)
        {
            return null;
        }

        return pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PregnancyLaborPushing)?.TryGetComp<MultipleBirthComp>()
            ?? pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PregnancyLabor)?.TryGetComp<MultipleBirthComp>()
            ?? pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PregnantHuman)?.TryGetComp<MultipleBirthComp>();
    }

    private static void CopyMultipleBirthComp(Hediff source, Hediff target)
    {
        MultipleBirthComp sourceComp = source?.TryGetComp<MultipleBirthComp>();
        MultipleBirthComp targetComp = target?.TryGetComp<MultipleBirthComp>();
        targetComp?.CopyFrom(sourceComp);
    }

    private static void RemoveDuplicateHediffsOfDef(Pawn pawn, HediffDef def)
    {
        if (pawn?.health?.hediffSet?.hediffs == null || def == null)
        {
            return;
        }

        List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
        Hediff keep = null;
        List<Hediff> duplicates = null;

        for (int i = 0; i < hediffs.Count; i++)
        {
            Hediff hediff = hediffs[i];
            if (hediff.def != def)
            {
                continue;
            }

            if (keep == null)
            {
                keep = hediff;
            }
            else
            {
                duplicates ??= new List<Hediff>();
                duplicates.Add(hediff);
            }
        }

        if (duplicates == null)
        {
            return;
        }

        for (int i = 0; i < duplicates.Count; i++)
        {
            pawn.health.RemoveHediff(duplicates[i]);
        }
    }
}
