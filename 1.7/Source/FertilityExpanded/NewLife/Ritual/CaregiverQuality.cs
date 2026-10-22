using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace FertilityExpanded;

public class CaregiverPresence : RitualOutcomeComp_ParticipantCount
{
    public bool wetNurse;

    private bool Matches(Pawn pawn, RitualObligation obligation)
    {
        return wetNurse ? FeedingBeliefs.IsWetNurseRolePawn(pawn)
            : RitualNewborns.IsParent(obligation, pawn);
    }

    public override float Count(LordJob_Ritual ritual, RitualOutcomeComp_Data data)
    {
        if (!(data is RitualOutcomeComp_DataThingPresence presence))
        {
            return 0f;
        }

        float duration = ritual.DurationTicks != 0 ? ritual.DurationTicks : ritual.TicksPassedWithProgress;
        foreach (KeyValuePair<Thing, float> entry in presence.presentForTicks)
        {
            if (entry.Key is Pawn pawn && entry.Value >= duration / 2f && Matches(pawn, ritual.obligation))
            {
                return 1f;
            }
        }

        return 0f;
    }

    public override QualityFactor GetQualityFactor(Precept_Ritual ritual, TargetInfo ritualTarget, RitualObligation obligation, RitualRoleAssignments assignments, RitualOutcomeComp_Data data)
    {
        bool present = false;
        foreach (Pawn pawn in assignments.Participants)
        {
            if (Matches(pawn, obligation))
            {
                present = true;
                break;
            }
        }

        float quality = curve.Evaluate(present ? 1f : 0f);
        return new QualityFactor {
            label = label.CapitalizeFirst(), present = present,
            quality = quality, qualityChange = ExpectedOffsetDesc(present, quality),
            positive = present, priority = 3f
        };
    }
}
