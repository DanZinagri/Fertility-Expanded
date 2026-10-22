using FertilityExpanded.Patches.Compatibility;
using HarmonyLib;
using RimWorld;
using Verse;

namespace FertilityExpanded;

public class WetNurseSource : HediffComp
{
    public override void CompPostPostAdd(DamageInfo? dinfo)
    {
        WetNurseLactation.MaintainLactation(Pawn);
    }

    public override void CompPostTickInterval(ref float severityAdjustment, int delta)
    {
        if (Pawn.IsHashIntervalTick(2500, delta))
        {
            WetNurseLactation.MaintainLactation(Pawn);
        }
    }
}

public class WetNurseSourceProperties : HediffCompProperties
{
    public WetNurseSourceProperties()
    {
        compClass = typeof(WetNurseSource);
    }
}
