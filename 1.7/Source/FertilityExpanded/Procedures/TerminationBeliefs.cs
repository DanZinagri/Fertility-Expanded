using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FertilityExpanded;

internal static class TerminationBeliefs
{
    private const string ForbiddenPreceptDefName = "FE_PregnancyTermination_Forbidden";

    private static PreceptDef forbiddenPrecept;

    public static bool IsForbiddenByIdeo(Pawn pawn)
    {
        return ModsConfig.IdeologyActive
            && ForbiddenPrecept != null
            && pawn?.Ideo?.HasPrecept(ForbiddenPrecept) == true;
    }

    public static bool IsViolation(Pawn pawn, Faction billDoerFaction)
    {
        if (pawn == null)
        {
            return false;
        }

        if (pawn.Faction == billDoerFaction && !pawn.IsQuestLodger())
        {
            return false;
        }

        return IsForbiddenByIdeo(pawn);
    }

    public static void ReportViolationIfForbidden(
        Pawn pawn,
        Pawn billDoer,
        Faction factionToInform,
        int goodwillImpact)
    {
        if (IsForbiddenByIdeo(pawn))
        {
            pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(FE_DefOf.FE_TerminatedPregnancyAgainstBelief);
        }

        if (billDoer != pawn && IsForbiddenByIdeo(billDoer))
        {
            billDoer.needs?.mood?.thoughts?.memories?.TryGainMemory(FE_DefOf.FE_PerformedTerminationAgainstBelief);
        }

        if (!IsViolation(pawn, Faction.OfPlayerSilentFail)
            || factionToInform == null
            || billDoer?.Faction != Faction.OfPlayer)
        {
            return;
        }

        Faction.OfPlayer.TryAffectGoodwillWith(
            factionToInform,
            goodwillImpact,
            canSendMessage: true,
            !factionToInform.temporary,
            HistoryEventDefOf.PerformedHarmfulSurgery);
        QuestUtility.SendQuestTargetSignals(pawn.questTags, "SurgeryViolation", pawn.Named("SUBJECT"));
    }

    private static PreceptDef ForbiddenPrecept => forbiddenPrecept ??= DefDatabase<PreceptDef>.GetNamedSilentFail(ForbiddenPreceptDefName);
}
