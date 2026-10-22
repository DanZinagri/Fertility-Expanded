using System.Collections.Generic;
using Verse;

namespace FertilityExpanded.Patches.Compatibility;

internal static class RjwFertilityPatch
{
    public static void Postfix(HediffSet diffSet, List<PawnCapacityUtility.CapacityImpactor> impactors, ref float __result)
    {
        Pawn pawn = diffSet?.pawn;
        if (pawn?.health?.hediffSet == null || __result <= 0f || ModDetection.RimJobWorldUsesVanillaPregnancy)
        {
            return;
        }

        float factor = FertilityExpandedHediffFactor(pawn);
        if (factor == 1f)
        {
            return;
        }

        __result *= factor;
        if (__result < 0f)
        {
            __result = 0f;
        }

        impactors?.Add(new RjwFertilityImpactor(factor));
    }

    private static float FertilityExpandedHediffFactor(Pawn pawn)
    {
        bool includeDrugEffects = !RjwDrugs.FertilityExpandedDrugsDisabled;
        float factor = 1f;
        List<Hediff> hediffs = pawn.health.hediffSet.hediffs;

        for (int i = 0; i < hediffs.Count; i++)
        {
            Hediff hediff = hediffs[i];
            if (hediff?.CurStage == null || !ShouldApplyHediff(hediff.def, includeDrugEffects))
            {
                continue;
            }

            factor *= hediff.CurStage.fertilityFactor;
        }

        return factor < 0f ? 0f : factor;
    }

    private static bool ShouldApplyHediff(HediffDef hediffDef, bool includeDrugEffects)
    {
        if (hediffDef == FE_DefOf.FE_FertilityCycle
            || hediffDef == FE_DefOf.FE_PostpartumRecovery
            || hediffDef == FE_DefOf.FE_MenopauseTransition
            || hediffDef == FE_DefOf.FE_Postmenopausal)
        {
            return true;
        }

        return includeDrugEffects
            && (hediffDef == FE_DefOf.FE_BirthControlActive
                || hediffDef == FE_DefOf.FE_HerbalContraception
                || hediffDef == FE_DefOf.FE_HerbalFertilityBoost
                || hediffDef == FE_DefOf.FE_FertilityTreatmentActive);
    }

    private class RjwFertilityImpactor : PawnCapacityUtility.CapacityImpactorPain
    {
        private readonly float factor;

        public RjwFertilityImpactor(float factor)
        {
            this.factor = factor;
        }

        public override string Readable(Pawn pawn)
        {
            return "FE_FertilityEffectsFactor".Translate(factor.ToStringPercent().Named("FACTOR"));
        }
    }
}
