using RimWorld;
using Verse;

namespace FertilityExpanded;

public class AdoptiveChildRelation : PawnRelationWorker
{
    public override bool InRelation(Pawn me, Pawn other)
    {
        return other?.relations?.DirectRelationExists(FE_DefOf.FE_AdoptiveParent, me) == true;
    }
}

public class FosterChildRelation : PawnRelationWorker
{
    public override bool InRelation(Pawn me, Pawn other)
    {
        return other?.relations?.DirectRelationExists(FE_DefOf.FE_FosterParent, me) == true;
    }
}
