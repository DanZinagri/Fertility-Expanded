using Verse;

namespace FertilityExpanded;

public class MenopauseTransition : HediffComp_Disappears
{
    public override void CompPostPostAdd(DamageInfo? dinfo)
    {
        base.CompPostPostAdd(dinfo);
        SetDuration(TransitionDuration());
    }

    public override void CompExposeData()
    {
        base.CompExposeData();
        if (Scribe.mode == LoadSaveMode.PostLoadInit && disappearsAfterTicks <= 0)
        {
            SetDuration(TransitionDuration());
        }
    }

    public override void CompPostPostRemoved()
    {
        base.CompPostPostRemoved();
        if (ticksToDisappear <= 0)
        {
            Menopause.CompleteMenopause(Pawn);
        }
    }

    private int TransitionDuration()
    {
        CycleComp cycle = Pawn.health.hediffSet.GetFirstHediffOfDef(FE_DefOf.FE_FertilityCycle)?.TryGetComp<CycleComp>();
        return cycle?.TicksUntilCycleEnd ?? Props.disappearsAfterTicks.RandomInRange;
    }
}

public class MenopauseTransitionProperties : HediffCompProperties_Disappears
{
    public MenopauseTransitionProperties()
    {
        compClass = typeof(MenopauseTransition);
    }
}
