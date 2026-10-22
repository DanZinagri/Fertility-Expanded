using RimWorld;
using RimWorld.Planet;
using Verse;

namespace FertilityExpanded;

public static class PlacementConsequences
{
    private const int AdoptionAcceptedGoodwill = 8;
    private const int FosterAcceptedGoodwill = 6;
    private const int FosterReturnedGoodwill = 20;
    private const int FosterKeptGoodwill = -60;
    private const string KinshipAdoptiveKinshipDefName = "FE_Kinship_AdoptiveKinship";

    private static PreceptDef kinshipAdoptiveKinship;

    public static void NotifyPlacementAccepted(ParentKind kind, Faction sourceFaction, Pawn child)
    {
        int goodwill = kind == ParentKind.Foster ? FosterAcceptedGoodwill : AdoptionAcceptedGoodwill;
        AffectSourceFactionGoodwill(sourceFaction, goodwill, FE_DefOf.FE_FamilyPlacementAccepted, child);
    }

    public static void NotifyPlacementRejected(ParentKind kind)
    {
        if (kind == ParentKind.Adoptive)
        {
            ApplyRefusedAdoptionThoughts();
        }
    }

    public static void NotifyFosterChildReturned(Faction sourceFaction, Pawn child)
    {
        AffectSourceFactionGoodwill(sourceFaction, FosterReturnedGoodwill, FE_DefOf.FE_FosterChildReturnedHome, child);
    }

    public static void NotifyFosterChildKept(Faction sourceFaction, Pawn child)
    {
        SendFosterChildKeptLetter(sourceFaction, child);
        AffectSourceFactionGoodwill(sourceFaction, FosterKeptGoodwill, FE_DefOf.FE_FosterChildKept, child);
    }

    private static void AffectSourceFactionGoodwill(Faction sourceFaction, int goodwill, HistoryEventDef reason, Pawn child)
    {
        if (!CanNotifySourceFaction(sourceFaction))
        {
            return;
        }

        GlobalTargetInfo target = child != null ? new GlobalTargetInfo(child) : GlobalTargetInfo.Invalid;
        Faction.OfPlayer.TryAffectGoodwillWith(sourceFaction, goodwill, canSendMessage: true, canSendHostilityLetter: true, reason, target);
    }

    private static void SendFosterChildKeptLetter(Faction sourceFaction, Pawn child)
    {
        if (!CanNotifySourceFaction(sourceFaction))
        {
            return;
        }

        string factionName = SourceFactionName(sourceFaction);
        TaggedString label = "FE_FosterChildKeptAngerLetterLabel".Translate();
        TaggedString text = "FE_FosterChildKeptAngerLetterText".Translate(child.Named("CHILD"), factionName.Named("FACTION"));
        Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.NegativeEvent, child, sourceFaction);
    }

    private static bool CanNotifySourceFaction(Faction sourceFaction)
    {
        return sourceFaction != null && !sourceFaction.IsPlayer && !sourceFaction.defeated && !sourceFaction.Hidden;
    }

    private static string SourceFactionName(Faction sourceFaction)
    {
        return sourceFaction != null && !sourceFaction.Name.NullOrEmpty()
            ? sourceFaction.Name
            : "FE_DistantSettlement".Translate();
    }

    private static void ApplyRefusedAdoptionThoughts()
    {
        if (!ModsConfig.IdeologyActive || FE_DefOf.FE_RefusedAdoptionRequest == null)
        {
            return;
        }

        foreach (Pawn pawn in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists)
        {
            if (pawn?.needs?.mood?.thoughts?.memories == null || pawn.Ideo?.HasPrecept(KinshipAdoptiveKinship) != true)
            {
                continue;
            }

            pawn.needs.mood.thoughts.memories.TryGainMemory(FE_DefOf.FE_RefusedAdoptionRequest);
        }
    }

    private static PreceptDef KinshipAdoptiveKinship => kinshipAdoptiveKinship ??= DefDatabase<PreceptDef>.GetNamedSilentFail(KinshipAdoptiveKinshipDefName);
}
