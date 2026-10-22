using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FertilityExpanded;

public class TerminatePregnancy : Recipe_Surgery
{
    private const int ForbiddenTerminationGoodwillImpact = -35;

    public override TaggedString GetConfirmation(Pawn pawn)
    {
        return TerminationBeliefs.IsForbiddenByIdeo(pawn)
            ? "FE_TerminationBeliefConfirmation".Translate(pawn.Named("PAWN"))
            : base.GetConfirmation(pawn);
    }

    public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
    {
        return ModsConfig.BiotechActive
            && thing is Pawn pawn
            && PregnancyUtility.GetPregnancyHediff(pawn) != null;
    }

    public override bool IsViolationOnPawn(Pawn pawn, BodyPartRecord part, Faction billDoerFaction)
    {
        return TerminationBeliefs.IsViolation(pawn, billDoerFaction);
    }

    public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
    {
        if (PregnancyUtility.GetPregnancyHediff(pawn) == null)
        {
            return;
        }

        if (billDoer != null && CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
        {
            return;
        }

        if (!PregnancyUtility.TryTerminatePregnancy(pawn))
        {
            return;
        }

        if (billDoer != null)
        {
            TaleRecorder.RecordTale(TaleDefOf.DidSurgery, billDoer, pawn);
        }

        if (PawnUtility.ShouldSendNotificationAbout(pawn))
        {
            Messages.Message("MessagePregnancyTerminated".Translate(pawn.Named("PAWN")), pawn, MessageTypeDefOf.PositiveEvent);
        }

        TerminationBeliefs.ReportViolationIfForbidden(
            pawn,
            billDoer,
            pawn.HomeFaction,
            ForbiddenTerminationGoodwillImpact);
    }
}
