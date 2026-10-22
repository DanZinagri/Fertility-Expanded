using Verse;
using System.Collections.Generic;
using RimWorld;

namespace FertilityExpanded;

public class CycleHediff : HediffWithComps
{
    public override string Description
    {
        get
        {
            HediffStage stage = CurStage;
            if (stage == null)
            {
                return base.Description;
            }

            string fertilityMultiplier = (stage.fertilityFactor * 100f).ToString("0");
            return "FE_CycleFertilityFactor".Translate(fertilityMultiplier.Named("FACTOR"));
        }
    }

    public override string Label
    {
        get
        {
            HediffStage stage = CurStage;
            string phase = stage?.label;
            return phase.NullOrEmpty()
                ? base.Label
                : phase.CapitalizeFirst();
        }
    }
}

public class CycleComp : HediffComp
{
    private const int TicksPerDay = 60000;
    private const int DefaultCycleTicks = 360000;
    private const float PeriodEndDay = 1f;
    private const float FollicularEndDay = 3f;
    private const float OvulationEndDay = 4f;

    private int cycleOffsetTicks = -1;

    private CycleProperties Props => (CycleProperties)props;

    internal int TicksUntilCycleEnd
    {
        get
        {
            int cycleTicks = Props.cycleTicks > 0 ? Props.cycleTicks : DefaultCycleTicks;
            return cycleTicks - GenMath.PositiveMod(Find.TickManager.TicksGame + cycleOffsetTicks, cycleTicks);
        }
    }

    public override void CompPostPostAdd(DamageInfo? dinfo)
    {
        base.CompPostPostAdd(dinfo);
        if (cycleOffsetTicks < 0)
        {
            cycleOffsetTicks = Rand.Range(0, Props.cycleTicks);
        }
        UpdateSeverity();
    }

    public override void CompPostTickInterval(ref float severityAdjustment, int delta)
    {
        base.CompPostTickInterval(ref severityAdjustment, delta);
        if (Pawn.IsHashIntervalTick(2500, delta))
        {
            UpdateSeverity();
        }
    }

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Values.Look(ref cycleOffsetTicks, "cycleOffsetTicks", -1);
    }

    public override IEnumerable<Gizmo> CompGetGizmos()
    {
        IEnumerable<Gizmo> baseGizmos = base.CompGetGizmos();
        if (baseGizmos != null)
        {
            foreach (Gizmo gizmo in baseGizmos)
            {
                yield return gizmo;
            }
        }

        if (!DebugSettings.ShowDevGizmos || Pawn == null)
        {
            yield break;
        }

        yield return new Command_Action
        {
            defaultLabel = "FE_DevStartMenopause".Translate(),
            action = delegate
            {
                Menopause.StartTransition(Pawn);
                Messages.Message("FE_DevMenopauseStarted".Translate(Pawn.Named("PAWN")), Pawn, MessageTypeDefOf.NeutralEvent, historical: false);
            }
        };

        yield return new Command_Action
        {
            defaultLabel = "FE_DevCompleteMenopause".Translate(),
            action = delegate
            {
                Menopause.CompleteMenopause(Pawn);
                Messages.Message("FE_DevMenopauseCompleted".Translate(Pawn.Named("PAWN")), Pawn, MessageTypeDefOf.NeutralEvent, historical: false);
            }
        };
    }

    private void UpdateSeverity()
    {
        int cycleTicks = Props.cycleTicks > 0 ? Props.cycleTicks : DefaultCycleTicks;
        int elapsedTicks = GenMath.PositiveMod(Find.TickManager.TicksGame + cycleOffsetTicks, cycleTicks);
        float cycleDay = elapsedTicks / (float)TicksPerDay;

        parent.Severity = cycleDay switch
        {
            < PeriodEndDay => 0.1f,
            < FollicularEndDay => 0.4f,
            < OvulationEndDay => 0.75f,
            _ => 0.9f
        };
    }
}

public class CycleProperties : HediffCompProperties
{
    public int cycleTicks = 360000;

    public CycleProperties()
    {
        compClass = typeof(CycleComp);
    }
}
