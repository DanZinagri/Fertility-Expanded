using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace FertilityExpanded;

public class ReverseVasectomy : Recipe_RemoveHediff
{
    private const float FailureStartAge = 30f;
    private const float FailureCapAge = 70f;
    private const float FailureChanceAtCap = 0.45f;

    public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
    {
        if (!Features.ProcedureOverrides)
        {
            base.ApplyOnPawn(pawn, part, billDoer, ingredients, bill);
            return;
        }

        if (billDoer != null)
        {
            if (CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
            {
                return;
            }

            TaleRecorder.RecordTale(TaleDefOf.DidSurgery, billDoer, pawn);

            if (Rand.Chance(AgeBasedFailureChance(pawn)))
            {
                NotifyReversalFailed(pawn, billDoer);
                return;
            }

            NotifyRemoved(pawn, billDoer);
        }

        RemoveTargetHediff(pawn, part);
    }

    private static float AgeBasedFailureChance(Pawn pawn)
    {
        float age = pawn?.ageTracker?.AgeBiologicalYearsFloat ?? 0f;
        if (age <= FailureStartAge)
        {
            return 0f;
        }

        return Mathf.Lerp(0f, FailureChanceAtCap, Mathf.InverseLerp(FailureStartAge, FailureCapAge, age));
    }

    private void NotifyReversalFailed(Pawn pawn, Pawn billDoer)
    {
        if (PawnUtility.ShouldSendNotificationAbout(pawn) || PawnUtility.ShouldSendNotificationAbout(billDoer))
        {
            Messages.Message("FE_ReverseVasectomyFailed".Translate(billDoer.Named("SURGEON"), pawn.Named("PATIENT")), pawn, MessageTypeDefOf.NeutralEvent);
        }
    }

    private void NotifyRemoved(Pawn pawn, Pawn billDoer)
    {
        if (!PawnUtility.ShouldSendNotificationAbout(pawn) && !PawnUtility.ShouldSendNotificationAbout(billDoer))
        {
            return;
        }

        string text = recipe.successfullyRemovedHediffMessage.NullOrEmpty()
            ? "MessageSuccessfullyRemovedHediff".Translate(billDoer.LabelShort, pawn.LabelShort, recipe.removesHediff.label.Named("HEDIFF"), billDoer.Named("SURGEON"), pawn.Named("PATIENT"))
            : recipe.successfullyRemovedHediffMessage.Formatted(billDoer.LabelShort, pawn.LabelShort);

        Messages.Message(text, pawn, MessageTypeDefOf.PositiveEvent);
    }

    private void RemoveTargetHediff(Pawn pawn, BodyPartRecord part)
    {
        if (recipe.targetsBodyPart)
        {
            Hediff hediff = pawn.health.hediffSet.hediffs.Find(x => x.def == recipe.removesHediff && x.Part == part && x.Visible);
            if (hediff != null)
            {
                pawn.health.RemoveHediff(hediff);
            }

            return;
        }

        for (int i = pawn.health.hediffSet.hediffs.Count - 1; i >= 0; i--)
        {
            Hediff hediff = pawn.health.hediffSet.hediffs[i];
            if (hediff.def == recipe.removesHediff && hediff.Visible)
            {
                pawn.health.RemoveHediff(hediff);
            }
        }
    }
}
