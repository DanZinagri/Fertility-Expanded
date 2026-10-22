using System.Collections.Generic;
using FertilityExpanded.Patches.Compatibility;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace FertilityExpanded;

public class MilkingJob : JobDriver
{
    private const int ChairSearchRadius = 32;
    private const int WorkTicks = 1000;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return true;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOn(() => !SelfMilking.CanAutomaticallyMilkSelf(pawn));

        yield return GoToChair();
        yield return SitInChair();

        Toil wait = Toils_General.Wait(WorkTicks);
        wait.WithProgressBarToilDelay(TargetIndex.A);
        wait.AddPreTickIntervalAction(delta => pawn.GainComfortFromCellIfPossible(delta, chairsOnly: true));
        yield return wait;

        Toil finish = ToilMaker.MakeToil("MilkSelfFinish");
        finish.initAction = delegate
        {
            if (!SelfMilking.TryMilkSelf(pawn, showMessages: false))
            {
                EndJobWith(JobCondition.Incompletable);
            }
        };
        finish.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return finish;
    }

    private Toil GoToChair()
    {
        Toil toil = ToilMaker.MakeToil("MilkSelfGoToChair");
        toil.initAction = delegate
        {
            IntVec3 selfMilkSpot = RCellFinder.SpotToStandDuringJob(pawn);
            Danger maxDanger = selfMilkSpot.IsValid
                ? selfMilkSpot.GetDangerFor(pawn, pawn.Map)
                : Danger.None;
            Thing chair = GenClosest.ClosestThingReachable(
                pawn.Position,
                pawn.Map,
                ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial),
                PathEndMode.OnCell,
                TraverseParms.For(pawn),
                ChairSearchRadius,
                thing => thing.def.building?.isSittable == true
                    && (int)thing.Position.GetDangerFor(pawn, thing.Map) <= (int)maxDanger
                    && IsValidSelfMilkChair(thing));
            if (chair != null)
            {
                Toils_Ingest.TryFindFreeSittingSpotOnThing(chair, pawn, out selfMilkSpot);
            }

            if (!selfMilkSpot.IsValid)
            {
                pawn.pather.StartPath(pawn.Position, PathEndMode.OnCell);
                return;
            }

            pawn.ReserveSittableOrSpot(selfMilkSpot, pawn.CurJob);
            pawn.Map.pawnDestinationReservationManager.Reserve(pawn, pawn.CurJob, selfMilkSpot);
            pawn.pather.StartPath(selfMilkSpot, PathEndMode.OnCell);
        };
        toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
        return toil;
    }

    private Toil SitInChair()
    {
        Toil toil = ToilMaker.MakeToil("MilkSelfSitInChair");
        toil.initAction = delegate
        {
            Thing chair = pawn.Spawned ? pawn.Position.GetEdifice(pawn.Map) : null;
            if (chair?.def.building?.isSittable == true)
            {
                pawn.Rotation = chair.Rotation;
            }
        };
        toil.defaultCompleteMode = ToilCompleteMode.Instant;
        return toil;
    }

    private bool IsValidSelfMilkChair(Thing thing)
    {
        return !thing.IsForbidden(pawn)
            && !thing.IsBurning()
            && !thing.HostileTo(pawn)
            && thing.IsSociallyProper(pawn)
            && pawn.CanReserve(thing)
            && Toils_Ingest.TryFindFreeSittingSpotOnThing(thing, pawn, out _);
    }
}
