using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FertilityExpanded;

public static class FosterResolution
{
    internal const int ResolutionAge = 18;
    internal const int ReviewDurationTicks = 3 * GenDate.TicksPerDay;

    public static bool SendReviewLetter(Pawn child, Faction sourceFaction)
    {
        if (!TryGetResolvablePlacement(child, out FosterPlacementRecord placement))
        {
            return false;
        }

        if (placement.reviewLetterSent)
        {
            return true;
        }

        sourceFaction = placement.sourceFaction ?? sourceFaction;
        string factionName = SourceFactionName(sourceFaction);
        TaggedString label = "FE_FosterResolutionLetterLabel".Translate(child.Named("CHILD"));
        TaggedString text = "FE_FosterResolutionLetterText".Translate(child.Named("CHILD"), factionName.Named("FACTION"));
        FosterResolutionLetter letter = LetterMaker.MakeLetter(
            label,
            text,
            FE_DefOf.FE_FosterResolutionLetter,
            child,
            sourceFaction) as FosterResolutionLetter;

        if (letter == null)
        {
            return false;
        }

        letter.Configure(child, sourceFaction);
        Find.LetterStack.ReceiveLetter(letter);
        placement.reviewLetterSent = true;
        placement.reviewDeadlineTick = Find.TickManager.TicksGame + ReviewDurationTicks;
        return true;
    }

    public static bool TryReturnHome(Pawn child, Faction sourceFaction)
    {
        if (!TryGetResolvablePlacement(child, out FosterPlacementRecord placement))
        {
            return false;
        }

        if (placement.reviewDeadlineTick >= 0 && Find.TickManager.TicksGame >= placement.reviewDeadlineTick)
        {
            Messages.Message("FE_FosterReturnExpired".Translate(), child, MessageTypeDefOf.RejectInput, false);
            return false;
        }

        sourceFaction = placement.sourceFaction ?? sourceFaction;
        Map rewardMap = child.MapHeld;
        if (sourceFaction == null)
        {
            Messages.Message("FE_FosterSourceMissing".Translate(), child, MessageTypeDefOf.RejectInput, false);
            return false;
        }
        Current.Game?.GetComponent<GameState>()?.RemoveFosterPlacement(child);

        if (sourceFaction != null && child.Faction != sourceFaction)
        {
            child.SetFaction(sourceFaction);
        }

        child.ExitMap(allowedToJoinOrCreateCaravan: false, Rot4.Random);

        PlacementConsequences.NotifyFosterChildReturned(sourceFaction, child);
        FosterReturnReward.TryCreateRewardQuest(child, sourceFaction, rewardMap);
        Messages.Message("FE_FosterChildReturned".Translate(child.Named("CHILD")), child, MessageTypeDefOf.NeutralEvent);
        return true;
    }

    public static bool TryAdoptFosterChild(Pawn child, Faction sourceFaction = null)
    {
        if (!TryGetResolvablePlacement(child, out FosterPlacementRecord placement))
        {
            return false;
        }

        sourceFaction = placement.sourceFaction ?? sourceFaction;
        Pawn fosterParent = null;
        foreach (DirectPawnRelation relation in child.relations.DirectRelations)
        {
            if (relation.def == FE_DefOf.FE_FosterParent && relation.otherPawn != null
                && !relation.otherPawn.Dead && !relation.otherPawn.Destroyed)
            {
                fosterParent = relation.otherPawn;
                break;
            }
        }
        RemoveFosterRelations(child);
        Current.Game?.GetComponent<GameState>()?.RemoveFosterPlacement(child);

        if (fosterParent != null && ParentAssignment.TryAssignParent(child, fosterParent, ParentKind.Adoptive))
        {
            Messages.Message("FE_FosterChildAdopted".Translate(child.Named("CHILD")), child, MessageTypeDefOf.PositiveEvent);
        }
        else
        {
            ParentAssignment.SendAssignmentLetter(child, ParentKind.Adoptive);
        }

        PlacementConsequences.NotifyFosterChildKept(sourceFaction, child);
        return true;
    }

    public static bool HasFosterParent(Pawn child)
    {
        return child?.relations != null
            && FE_DefOf.FE_FosterParent != null
            && child.relations.GetDirectRelationsCount(FE_DefOf.FE_FosterParent) > 0;
    }

    internal static bool HasLivingFosterParent(Pawn child)
    {
        if (child?.relations == null || FE_DefOf.FE_FosterParent == null)
        {
            return false;
        }

        foreach (DirectPawnRelation relation in child.relations.DirectRelations)
        {
            if (relation.def == FE_DefOf.FE_FosterParent
                && relation.otherPawn != null && !relation.otherPawn.Dead && !relation.otherPawn.Destroyed)
            {
                return true;
            }
        }

        return false;
    }

    internal static void CheckGuardian(FosterPlacementRecord placement)
    {
        Pawn child = placement?.child;
        if (placement?.Active == true && child.ageTracker.AgeBiologicalYears < ResolutionAge
            && !HasLivingFosterParent(child))
        {
            ParentAssignment.SendAssignmentLetter(child, ParentKind.Foster);
        }
    }

    public static bool CanResolvePlacement(Pawn child)
    {
        return child?.ageTracker?.AgeBiologicalYears >= ResolutionAge
            && TryGetResolvablePlacement(child, out _);
    }

    internal static bool TryGetResolvablePlacement(Pawn child, out FosterPlacementRecord placement)
    {
        placement = null;
        if (child == null
            || child.Destroyed
            || child.Dead
            || child.Faction != Faction.OfPlayer
            || child.relations == null
            || !child.Spawned
            || child.MapHeld?.IsPlayerHome != true)
        {
            return false;
        }

        GameState component = Current.Game?.GetComponent<GameState>();
        return component?.TryGetActiveFosterPlacement(child, out placement) == true;
    }

    private static void RemoveFosterRelations(Pawn child)
    {
        List<DirectPawnRelation> relations = child.relations.DirectRelations;
        for (int i = relations.Count - 1; i >= 0; i--)
        {
            DirectPawnRelation relation = relations[i];
            if (relation.def == FE_DefOf.FE_FosterParent && relation.otherPawn != null)
            {
                child.relations.TryRemoveDirectRelation(FE_DefOf.FE_FosterParent, relation.otherPawn);
            }
        }
    }

    private static string SourceFactionName(Faction sourceFaction)
    {
        return sourceFaction != null && !sourceFaction.Name.NullOrEmpty()
            ? sourceFaction.Name
            : "FE_DistantSettlement".Translate();
    }
}
