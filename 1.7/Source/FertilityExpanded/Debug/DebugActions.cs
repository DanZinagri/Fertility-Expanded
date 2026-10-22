using System.Collections.Generic;
using LudeonTK;
using RimWorld;
using Verse;

namespace FertilityExpanded;

internal static class DebugActions
{
    [DebugActionYielder]
    private static IEnumerable<DebugActionNode> Actions()
    {
        string category = "FE_ModName".Translate();
        DebugActionAttribute availability = new DebugActionAttribute
        {
            allowedGameStates = AllowedGameStates.PlayingOnMap,
            requiresBiotech = true
        };

        yield return new DebugActionNode("FE_DevMenuEmergencyAdoption".Translate(), action: ForceEmergencyAdoptionRequest)
        {
            category = category,
            sourceAttribute = availability
        };
        yield return new DebugActionNode("FE_DevMenuFosterPlacement".Translate(), action: ForceFosterPlacementRequest)
        {
            category = category,
            sourceAttribute = availability
        };
        yield return new DebugActionNode("FE_DevMenuFosterReview".Translate(), DebugActionType.ToolMapForPawns, pawnAction: ForceFosterReviewForPawn)
        {
            category = category,
            sourceAttribute = availability
        };
        yield return new DebugActionNode("FE_DevMenuOrphanReview".Translate(), DebugActionType.ToolMapForPawns, pawnAction: ForceOrphanAdoptionReviewForPawn)
        {
            category = category,
            sourceAttribute = availability
        };
        yield return new DebugActionNode("FE_DevMenuSingleBirth".Translate(), DebugActionType.ToolMapForPawns, pawnAction: ForceSingleBirthOnPawn)
        {
            category = category,
            sourceAttribute = availability
        };
        yield return new DebugActionNode("FE_DevMenuTwins".Translate(), DebugActionType.ToolMapForPawns, pawnAction: ForceTwinsOnPawn)
        {
            category = category,
            sourceAttribute = availability
        };
        yield return new DebugActionNode("FE_DevMenuTriplets".Translate(), DebugActionType.ToolMapForPawns, pawnAction: ForceTripletsOnPawn)
        {
            category = category,
            sourceAttribute = availability
        };
    }

    private static void ForceEmergencyAdoptionRequest()
    {
        SendPlacementRequest(ParentKind.Adoptive);
    }

    private static void ForceFosterPlacementRequest()
    {
        SendPlacementRequest(ParentKind.Foster);
    }

    private static void ForceFosterReviewForPawn(Pawn pawn)
    {
        if (Current.Game?.GetComponent<GameState>()?.HasActiveFosterPlacement(pawn) != true)
        {
            Reject(pawn, "FE_DevNotFoster".Translate());
            return;
        }

        Faction sourceFaction = RandomSourceFaction();
        if (FosterResolution.SendReviewLetter(pawn, sourceFaction))
        {
            Messages.Message("FE_DevFosterReviewSent".Translate(), pawn, MessageTypeDefOf.NeutralEvent, historical: false);
        }
        else
        {
            Reject(pawn, "FE_DevFosterReviewFailed".Translate());
        }
    }

    private static void ForceOrphanAdoptionReviewForPawn(Pawn pawn)
    {
        if (Current.Game?.GetComponent<GameState>()?.HasActiveFosterPlacement(pawn) == true)
        {
            Reject(pawn, "FE_DevActiveFoster".Translate());
            return;
        }

        if (OrphanAdoption.TrySendPrompt(pawn, out string rejectReason))
        {
            Messages.Message("FE_DevOrphanReviewSent".Translate(), pawn, MessageTypeDefOf.NeutralEvent, historical: false);
        }
        else
        {
            Reject(pawn, "FE_DevRejectReason".Translate(rejectReason.Named("REASON")));
        }
    }

    private static void ForceSingleBirthOnPawn(Pawn pawn)
    {
        ForcePregnancyBabyCount(pawn, 1);
    }

    private static void ForceTwinsOnPawn(Pawn pawn)
    {
        ForcePregnancyBabyCount(pawn, 2);
    }

    private static void ForceTripletsOnPawn(Pawn pawn)
    {
        ForcePregnancyBabyCount(pawn, 3);
    }

    private static void SendPlacementRequest(ParentKind kind)
    {
        Map map = Find.CurrentMap;
        if (map == null)
        {
            Messages.Message("FE_DevNoMap".Translate(), MessageTypeDefOf.RejectInput, historical: false);
            return;
        }

        FamilyPlacement.SendPlacementLetter(map, kind, RandomSourceFaction());

        string message = kind == ParentKind.Foster
            ? "FE_DevFosterRequestSent".Translate()
            : "FE_DevAdoptionRequestSent".Translate();
        Messages.Message(message, new TargetInfo(map.Center, map), MessageTypeDefOf.NeutralEvent, historical: false);
    }

    private static void ForcePregnancyBabyCount(Pawn pawn, int babyCount)
    {
        if (!MultipleBirths.TrySetPregnancyBabyCountForDebug(pawn, babyCount))
        {
            Reject(pawn, "FE_DevSelectPregnant".Translate());
            return;
        }

        Messages.Message("FE_DevBirthCount".Translate(pawn.Named("PAWN"), babyCount.Named("COUNT")), pawn, MessageTypeDefOf.NeutralEvent, historical: false);
    }

    private static Faction RandomSourceFaction()
    {
        return Find.FactionManager.RandomNonHostileFaction(allowHidden: false, allowDefeated: false, allowNonHumanlike: false);
    }

    private static void Reject(Pawn pawn, string text)
    {
        if (pawn == null)
        {
            Messages.Message(text, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }

        Messages.Message(text, pawn, MessageTypeDefOf.RejectInput, historical: false);
    }
}
