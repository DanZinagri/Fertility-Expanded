using FertilityExpanded.Patches.Compatibility;
using RimWorld;
using Verse;

namespace FertilityExpanded;

public class LactationImplant : HediffComp
{
    private const int RefreshIntervalTicks = 2500;
    private const float MaintainedSeverity = 1f;

    public override void CompPostPostAdd(DamageInfo? dinfo)
    {
        EnsureLactating();
    }

    public override void CompPostTickInterval(ref float severityAdjustment, int delta)
    {
        if (Pawn?.Dead == false && Pawn.IsHashIntervalTick(RefreshIntervalTicks, delta))
        {
            EnsureLactating();
        }
    }

    private void EnsureLactating()
    {
        if (!Features.LactationSystem
            || !ModsConfig.BiotechActive
            || ModDetection.LactationImplantMaintenanceBlocked
            || Pawn?.health?.hediffSet == null
            || Pawn.Dead
            || Pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Malnutrition)?.CurStageIndex >= 2)
        {
            return;
        }

        Hediff lactating = Pawn.health.GetOrAddHediff(HediffDefOf.Lactating);
        if (lactating.Severity < MaintainedSeverity)
        {
            lactating.Severity = MaintainedSeverity;
        }
    }
}

public class LactationImplantProperties : HediffCompProperties
{
    public LactationImplantProperties()
    {
        compClass = typeof(LactationImplant);
    }
}
