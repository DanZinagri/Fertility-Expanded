using RimWorld;
using Verse;

namespace FertilityExpanded;

public class FosterPlacementRecord : IExposable
{
    public Pawn child;
    public Faction sourceFaction;
    public bool reviewLetterSent;
    public int reviewDeadlineTick = -1;

    public FosterPlacementRecord()
    {
    }

    public FosterPlacementRecord(Pawn child, Faction sourceFaction)
    {
        this.child = child;
        this.sourceFaction = sourceFaction;
    }

    public bool Active => child != null
        && !child.Destroyed
        && !child.Dead
        && child.Faction == Faction.OfPlayer;

    public bool ReadyForReview => Active
        && !reviewLetterSent
        && FosterResolution.CanResolvePlacement(child);

    public void ExposeData()
    {
        Scribe_References.Look(ref child, "child", saveDestroyedThings: true);
        Scribe_References.Look(ref sourceFaction, "sourceFaction");
        Scribe_Values.Look(ref reviewLetterSent, "reviewLetterSent", defaultValue: false);
        Scribe_Values.Look(ref reviewDeadlineTick, "reviewDeadlineTick", -1);
    }
}
