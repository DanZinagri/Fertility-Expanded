using System.Collections.Generic;
using FertilityExpanded.Patches.Compatibility;
using RimWorld;
using Verse;

namespace FertilityExpanded;

public class FertilityDisplay : PawnCapacityWorker
{
    public override float CalculateCapacityLevel(HediffSet diffSet, List<PawnCapacityUtility.CapacityImpactor> impactors = null)
    {
        Pawn pawn = diffSet?.pawn;
        if (pawn == null || !ModsConfig.BiotechActive)
        {
            return 0f;
        }

        float fertility = pawn.GetStatValue(StatDefOf.Fertility);
        impactors?.Add(new FertilityImpactor(fertility));
        return fertility;
    }

    public override bool CanHaveCapacity(BodyDef body)
    {
        return ModsConfig.BiotechActive
            && !ModDetection.RimJobWorldActive;
    }

    private class FertilityImpactor : PawnCapacityUtility.CapacityImpactorPain
    {
        private readonly float fertility;

        public FertilityImpactor(float fertility)
        {
            this.fertility = fertility;
        }

        public override string Readable(Pawn pawn)
        {
            return "FE_BiotechFertilityStat".Translate(fertility.ToStringPercent().Named("VALUE"));
        }
    }
}
