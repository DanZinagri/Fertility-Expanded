using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace FertilityExpanded;

public class NewLifeTarget : RitualObligationTargetWorker_AnyRitualSpot
{
    public NewLifeTarget()
    {
    }

    public NewLifeTarget(RitualObligationTargetFilterDef def) : base(def)
    {
    }

    public override bool ObligationTargetsValid(RitualObligation obligation)
    {
        bool anyLiving = false;
        for (int i = 0; i < 3; i++)
        {
            Pawn baby = RitualNewborns.At(obligation, i);
            if (!RitualNewborns.Living(baby))
            {
                continue;
            }
            if (baby.Faction != Faction.OfPlayer)
            {
                return false;
            }
            anyLiving = true;
        }
        return anyLiving;
    }

    public override string LabelExtraPart(RitualObligation obligation)
    {
        return RitualNewborns.Labels(obligation);
    }

    public override IEnumerable<string> GetTargetInfos(RitualObligation obligation)
    {
        for (int i = 0; i < 3; i++)
        {
            Pawn baby = RitualNewborns.At(obligation, i);
            if (RitualNewborns.Living(baby))
            {
                yield return "FE_RitualNewborn".Translate(baby.Named("BABY"));
            }
        }

        foreach (string info in base.GetTargetInfos(obligation))
        {
            yield return info;
        }
    }

    public override IEnumerable<TargetInfo> GetTargets(RitualObligation obligation, Map map)
    {
        if (map == null || !ObligationTargetsValid(obligation))
        {
            yield break;
        }

        for (int i = 0; i < 3; i++)
        {
            Pawn baby = RitualNewborns.At(obligation, i);
            if (RitualNewborns.Living(baby) && baby.MapHeld != map)
            {
                yield break;
            }
        }

        foreach (TargetInfo target in base.GetTargets(obligation, map))
        {
            yield return target;
        }
    }

    protected override RitualTargetUseReport CanUseTargetInternal(TargetInfo target, RitualObligation obligation)
    {
        if (obligation?.StillValid != true || !base.CanUseTargetInternal(target, obligation).canUse
            || !ObligationTargetsValid(obligation))
        {
            return false;
        }

        return GroupCareReport(target, obligation);
    }

    internal static RitualTargetUseReport GroupCareReport(TargetInfo target, RitualObligation obligation)
    {
        bool anyLiving = false;
        for (int i = 0; i < 3; i++)
        {
            Pawn baby = RitualNewborns.At(obligation, i);
            if (!RitualNewborns.Living(baby))
            {
                continue;
            }
            anyLiving = true;
            RitualTargetUseReport report = NewbornCareReport(target, baby);
            if (!report.canUse)
            {
                return report;
            }
        }
        return anyLiving;
    }

    internal static RitualTargetUseReport NewbornCareReport(TargetInfo target, Pawn baby)
    {
        if (baby == null || baby.Destroyed || baby.Dead || baby.Faction != Faction.OfPlayer
            || target.Thing?.def != ThingDefOf.RitualSpot || target.Map == null)
        {
            return false;
        }

        if (baby.MapHeld != target.Map || !baby.PositionHeld.InHorDistOf(target.Cell, 12f))
        {
            return "FE_NewLifeBabyTooFar".Translate(baby.Named("BABY"));
        }

        if (baby.CarriedBy != null || (baby.CurrentBed()?.def.building?.bed_maxBodySize ?? float.MaxValue) <= 0.25f)
        {
            return true;
        }

        return "FE_NewLifeBabyNeedsCare".Translate(baby.Named("BABY"));
    }
}
