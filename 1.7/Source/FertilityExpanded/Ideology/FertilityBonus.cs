using RimWorld;
using Verse;

namespace FertilityExpanded;

public class CommunityFertility : StatPart
{
    private const float FertilityFactor = 1.10f;

    public override void TransformValue(StatRequest req, ref float val)
    {
        if (req.Thing is Pawn pawn
            && CradleCommunity.HasCradleCommunity(pawn))
        {
            val *= FertilityFactor;
        }
    }

    public override string ExplanationPart(StatRequest req)
    {
        if (req.Thing is Pawn pawn
            && CradleCommunity.HasCradleCommunity(pawn))
        {
            return "FE_CradleFertilityFactor".Translate(FertilityFactor.ToStringPercent().Named("FACTOR"));
        }

        return null;
    }
}
