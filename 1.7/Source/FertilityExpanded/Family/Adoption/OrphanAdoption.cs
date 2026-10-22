using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FertilityExpanded;

public static class OrphanAdoption
{
    public const int GracePeriodTicks = 60000;
    public const int ReminderCooldownTicks = 180000;

    public static bool TrySendPrompt(Pawn child, out string rejectReason)
    {
        if (!CanSendPrompt(child, out rejectReason))
        {
            return false;
        }

        if (!ParentAssignment.SendAssignmentLetter(child, ParentKind.Adoptive, ParentReason.Orphaned))
        {
            rejectReason = "FE_OrphanNoCandidate".Translate();
            return false;
        }

        rejectReason = null;
        return true;
    }

    public static bool CanSendPrompt(Pawn child, out string rejectReason)
    {
        if (!IsEligibleOrphanChild(child, out rejectReason))
        {
            return false;
        }

        if (!IsStablePromptLocation(child))
        {
            rejectReason = "FE_OrphanUnstableLocation".Translate();
            return false;
        }

        if (!ParentAssignment.CanAssignAnyParent(child, ParentKind.Adoptive))
        {
            rejectReason = "FE_OrphanNoCandidate".Translate();
            return false;
        }

        rejectReason = null;
        return true;
    }

    public static bool IsEligibleOrphanChild(Pawn child)
    {
        return IsEligibleOrphanChild(child, out _);
    }

    public static bool IsEligibleOrphanChild(Pawn child, out string rejectReason)
    {
        if (!ModsConfig.BiotechActive)
        {
            rejectReason = "FE_OrphanBiotechInactive".Translate();
            return false;
        }

        if (child == null || child.Destroyed || child.Dead)
        {
            rejectReason = "FE_OrphanInvalidPawn".Translate();
            return false;
        }

        if (!child.RaceProps.Humanlike || child.relations == null)
        {
            rejectReason = "FE_OrphanNotHumanlike".Translate();
            return false;
        }

        if (!child.IsFreeColonist)
        {
            rejectReason = "FE_OrphanNotColonist".Translate();
            return false;
        }

        if (!IsBabyOrChild(child))
        {
            rejectReason = "FE_OrphanNotChild".Translate();
            return false;
        }

        if (HasAnyFosterParentRelation(child))
        {
            rejectReason = "FE_OrphanFosterPlacement".Translate();
            return false;
        }

        if (HasLivingRecognizedParent(child))
        {
            rejectReason = "FE_OrphanHasParent".Translate();
            return false;
        }

        rejectReason = null;
        return true;
    }

    public static bool HasLivingRecognizedParent(Pawn child)
    {
        if (child?.relations == null)
        {
            return false;
        }

        List<DirectPawnRelation> relations = child.relations.DirectRelations;
        for (int i = 0; i < relations.Count; i++)
        {
            DirectPawnRelation relation = relations[i];
            if (FamilyRelations.IsParentLikeRelation(relation.def) && IsLivingPawn(relation.otherPawn))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsStablePromptLocation(Pawn child)
    {
        Map map = child?.MapHeld;
        return map == null || map.IsPlayerHome;
    }

    private static bool IsBabyOrChild(Pawn pawn)
    {
        return pawn.DevelopmentalStage.Baby() || pawn.DevelopmentalStage.Child();
    }

    private static bool HasAnyFosterParentRelation(Pawn child)
    {
        return child?.relations != null
            && FE_DefOf.FE_FosterParent != null
            && child.relations.GetDirectRelationsCount(FE_DefOf.FE_FosterParent) > 0;
    }

    private static bool IsLivingPawn(Pawn pawn)
    {
        return pawn != null && !pawn.Destroyed && !pawn.Dead;
    }
}
