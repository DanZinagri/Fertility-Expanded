using Verse;

namespace FertilityExpanded;

public class OrphanAdoptionRecord : IExposable
{
    public Pawn child;
    public int firstEligibleTick;
    public int nextPromptTick;

    public OrphanAdoptionRecord()
    {
    }

    public OrphanAdoptionRecord(Pawn child, int currentTick)
    {
        this.child = child;
        firstEligibleTick = currentTick;
        nextPromptTick = currentTick + OrphanAdoption.GracePeriodTicks;
    }

    public void ExposeData()
    {
        Scribe_References.Look(ref child, "child", saveDestroyedThings: true);
        Scribe_Values.Look(ref firstEligibleTick, "firstEligibleTick");
        Scribe_Values.Look(ref nextPromptTick, "nextPromptTick");
    }
}
