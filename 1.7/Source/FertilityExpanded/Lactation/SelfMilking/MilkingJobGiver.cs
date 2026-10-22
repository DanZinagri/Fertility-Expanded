using System.Collections.Generic;
using FertilityExpanded.Patches.Compatibility;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace FertilityExpanded;

public class MilkingJobGiver : ThinkNode_JobGiver
{
    public bool requireFullStorage;

    public override ThinkNode DeepCopy(bool resolve = true)
    {
        MilkingJobGiver copy = (MilkingJobGiver)base.DeepCopy(resolve);
        copy.requireFullStorage = requireFullStorage;
        return copy;
    }

    protected override Job TryGiveJob(Pawn pawn)
    {
        bool continuing = pawn?.CurJobDef == FE_DefOf.FE_MilkSelf;
        if (!SelfMilking.CanAutomaticallyMilkSelf(pawn, requireFullStorage && !continuing))
        {
            return null;
        }

        return continuing ? pawn.CurJob : JobMaker.MakeJob(FE_DefOf.FE_MilkSelf, pawn);
    }
}
