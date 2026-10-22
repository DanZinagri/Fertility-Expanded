using Verse;
using RimWorld;

namespace FertilityExpanded;

public class MultipleBirthGene : Gene
{
    public const float MultipleBirthChanceFactor = 3f;

    public override bool Active => pawn?.gender == Gender.Female && base.Active;
}

internal static class GeneCommonality
{
    public static void TryApply(Pawn pawn)
    {
        if (pawn?.RaceProps?.Humanlike != true
            || pawn.genes == null
            || pawn.DevelopmentalStage.Newborn()
            || pawn.genes.UniqueXenotype
            || (pawn.gender != Gender.Female && pawn.gender != Gender.Male)
            || pawn.genes.GetGene(FE_DefOf.FE_MultipleBirthTendency) != null)
        {
            return;
        }

        float commonality = Features.MultibirthGeneCommonality;
        if (commonality <= 0f
            || (commonality < 1f && !Rand.Chance(commonality)))
        {
            return;
        }

        pawn.genes.AddGene(FE_DefOf.FE_MultipleBirthTendency, xenogene: false);
    }
}
