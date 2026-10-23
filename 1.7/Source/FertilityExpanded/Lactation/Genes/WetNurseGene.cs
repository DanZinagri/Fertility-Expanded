using Verse;

namespace FertilityExpanded;

// Keeps the carrier lactating, resuming once severe malnutrition clears. The GeneDef
// is removed by the Mods/LactationExpansion patch folder when Lactation Expansion is active, since it ships its own.
public class WetNurseGene : Gene
{
    // Looked up by name: the def is absent while Lactation Expansion is active.
    private static GeneDef geneDef;
    private static bool lookedForGeneDef;

    public static bool IsActiveCarrier(Pawn pawn)
    {
        if (!lookedForGeneDef)
        {
            lookedForGeneDef = true;
            geneDef = DefDatabase<GeneDef>.GetNamedSilentFail("FE_AlwaysLactating");
        }

        return geneDef != null && pawn.genes?.HasActiveGene(geneDef) == true;
    }

    public override void PostAdd()
    {
        base.PostAdd();
        if (Active)
        {
            WetNurseLactation.MaintainLactation(pawn, Features.MaleLactationFromGene);
        }
    }

    public override void TickInterval(int delta)
    {
        base.TickInterval(delta);
        if (pawn.IsHashIntervalTick(2500, delta) && Active)
        {
            WetNurseLactation.MaintainLactation(pawn, Features.MaleLactationFromGene);
        }
    }
}
