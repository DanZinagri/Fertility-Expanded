using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace FertilityExpanded;

public class NewLifeRitual : RitualBehaviorWorker
{
    public NewLifeRitual()
    {
    }

    public NewLifeRitual(RitualBehaviorDef def)
        : base(def)
    {
    }

    public override void TryExecuteOn(TargetInfo target, Pawn organizer, Precept_Ritual ritual, RitualObligation obligation, RitualRoleAssignments assignments, bool playerForced = false)
    {
        if (obligation?.StillValid != true || !NewLifeTarget.GroupCareReport(target, obligation).canUse)
        {
            Messages.Message("FE_NewLifeNotReady".Translate(), target, MessageTypeDefOf.RejectInput, false);
            return;
        }

        if (ParticipatingCarrier(obligation, assignments))
        {
            Messages.Message("FE_NewLifeCarrierParticipating".Translate(), target, MessageTypeDefOf.RejectInput, false);
            return;
        }

        base.TryExecuteOn(target, organizer, ritual, obligation, assignments, playerForced);
    }

    public override bool TargetStillAllowed(TargetInfo selectedTarget, LordJob_Ritual ritual)
    {
        return NewLifeTarget.GroupCareReport(selectedTarget, ritual.obligation).canUse
            && !ParticipatingCarrier(ritual.obligation, ritual.assignments);
    }

    private static bool ParticipatingCarrier(RitualObligation obligation, RitualRoleAssignments assignments)
    {
        for (int i = 0; i < 3; i++)
        {
            Pawn baby = RitualNewborns.At(obligation, i);
            if (RitualNewborns.Living(baby) && baby.CarriedBy != null && assignments.PawnParticipating(baby.CarriedBy))
            {
                return true;
            }
        }
        return false;
    }
}
