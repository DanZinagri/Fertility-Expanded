using FertilityExpanded.Patches.Compatibility;
using HarmonyLib;
using RimWorld;
using Verse;

namespace FertilityExpanded;

public class WetNurseSource : HediffComp
{
    public override void CompPostPostAdd(DamageInfo? dinfo)
    {
        if (ModsConfig.IdeologyActive)
        {
            WetNurseLactation.MaintainLactation(Pawn, Features.MaleLactationFromRole);
        }
    }

    public override void CompPostTickInterval(ref float severityAdjustment, int delta)
    {
        if (ModsConfig.IdeologyActive && Pawn.IsHashIntervalTick(2500, delta))
        {
            WetNurseLactation.MaintainLactation(Pawn, Features.MaleLactationFromRole);
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
