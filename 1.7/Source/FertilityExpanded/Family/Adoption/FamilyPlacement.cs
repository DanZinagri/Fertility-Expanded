using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace FertilityExpanded;

public static class FamilyPlacement
{
    public static void SendPlacementLetter(Map map, ParentKind kind, Faction sourceFaction)
    {
        if (map == null || !ModsConfig.BiotechActive)
        {
            return;
        }

        string factionName = SourceFactionName(sourceFaction);
        TaggedString label = kind == ParentKind.Foster
            ? "FE_FosterPlacementLetterLabel".Translate()
            : "FE_AdoptionPlacementLetterLabel".Translate();
        TaggedString text = kind == ParentKind.Foster
            ? "FE_FosterPlacementLetterText".Translate(factionName.Named("FACTION"))
            : "FE_AdoptionPlacementLetterText".Translate(factionName.Named("FACTION"));

        PlacementOfferLetter letter = LetterMaker.MakeLetter(
            label,
            text,
            FE_DefOf.FE_FamilyPlacementLetter,
            new TargetInfo(map.Center, map),
            sourceFaction) as PlacementOfferLetter;

        if (letter == null)
        {
            return;
        }

        letter.Configure(kind);
        Find.LetterStack.ReceiveLetter(letter);
    }

    public static bool TryAcceptPlacement(Map map, ParentKind kind, Faction sourceFaction, out Pawn child)
    {
        child = null;
        if (map == null || !ModsConfig.BiotechActive || !TryFindArrivalCell(map, out IntVec3 cell))
        {
            return false;
        }

        child = GenerateChild(map, kind);
        if (child == null)
        {
            return false;
        }

        child.ageTracker.AgeChronologicalTicks = child.ageTracker.AgeBiologicalTicks;
        child.needs?.SetInitialLevels();
        DropPodUtility.DropThingsNear(
            cell,
            map,
            new List<Thing> { child },
            openDelay: 80,
            canInstaDropDuringInit: false,
            leaveSlag: false,
            canRoofPunch: false,
            forbid: false,
            allowFogged: false,
            faction: null);

        if (kind == ParentKind.Foster)
        {
            Current.Game?.GetComponent<GameState>()?.RegisterFosterPlacement(child, sourceFaction);
        }
        ParentAssignment.SendAssignmentLetter(child, kind);

        TaggedString message = kind == ParentKind.Foster
            ? "FE_FosterChildArrived".Translate(child.Named("CHILD"))
            : "FE_AdoptiveChildArrived".Translate(child.Named("CHILD"));
        Messages.Message(message, new TargetInfo(cell, map), MessageTypeDefOf.PositiveEvent);
        return true;
    }

    private static Pawn GenerateChild(Map map, ParentKind kind)
    {
        DevelopmentalStage stage = kind == ParentKind.Foster ? DevelopmentalStage.Child : DevelopmentalStage.Baby;
        FloatRange? biologicalAgeRange = kind == ParentKind.Foster ? new FloatRange(7f, 12f) : null;
        float? fixedAge = biologicalAgeRange?.RandomInRange;

        PawnGenerationRequest request = new PawnGenerationRequest(
            PawnKindDefOf.SpaceRefugee,
            Faction.OfPlayer,
            PawnGenerationContext.NonPlayer,
            map.Tile,
            forceGenerateNewPawn: true,
            allowDead: false,
            allowDowned: kind == ParentKind.Adoptive,
            canGeneratePawnRelations: false,
            mustBeCapableOfViolence: false,
            colonistRelationChanceFactor: 0f,
            allowPregnant: false,
            allowAddictions: false,
            fixedBiologicalAge: fixedAge,
            fixedChronologicalAge: fixedAge,
            developmentalStages: stage,
            forceRecruitable: true,
            forceNoGear: true);

        return PawnGenerator.GeneratePawn(request);
    }

    private static string SourceFactionName(Faction sourceFaction)
    {
        return sourceFaction != null && !sourceFaction.Name.NullOrEmpty()
            ? sourceFaction.Name
            : "FE_DistantSettlement".Translate();
    }

    private static bool TryFindArrivalCell(Map map, out IntVec3 cell)
    {
        cell = DropCellFinder.TradeDropSpot(map);
        if (cell.IsValid && cell.Standable(map) && !cell.Fogged(map))
        {
            return true;
        }

        return CellFinder.TryFindRandomCell(map, c => c.Standable(map) && !c.Fogged(map), out cell);
    }
}
