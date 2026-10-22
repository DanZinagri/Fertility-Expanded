using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FertilityExpanded;

public class PlacementOfferLetter : ChoiceLetter
{
    private ParentKind assignmentKind;

    public override IEnumerable<DiaOption> Choices
    {
        get
        {
            if (!base.ArchivedOnly)
            {
                yield return new DiaOption("Accept".Translate())
                {
                    action = AcceptPlacement,
                    resolveTree = true
                };

                yield return new DiaOption("RejectLetter".Translate())
                {
                    action = delegate
                    {
                        PlacementConsequences.NotifyPlacementRejected(assignmentKind);
                        Find.LetterStack.RemoveLetter(this);
                    },
                    resolveTree = true
                };
            }

            if (base.ArchivedOnly)
            {
                yield return base.Option_Close;
            }
        }
    }

    public void Configure(ParentKind assignmentKind)
    {
        this.assignmentKind = assignmentKind;
        title = assignmentKind == ParentKind.Foster
            ? "FE_FosterPlacementTitle".Translate()
            : "FE_AdoptionPlacementTitle".Translate();
        StartTimeout(GenDate.TicksPerDay * 3);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref assignmentKind, "assignmentKind", ParentKind.Adoptive);
    }

    private void AcceptPlacement()
    {
        Map map = lookTargets.TryGetPrimaryTarget().Map ?? Find.CurrentMap;
        if (FamilyPlacement.TryAcceptPlacement(map, assignmentKind, relatedFaction, out Pawn child))
        {
            PlacementConsequences.NotifyPlacementAccepted(assignmentKind, relatedFaction, child);
            Find.LetterStack.RemoveLetter(this);
            return;
        }

        Messages.Message("FE_FamilyPlacementFailed".Translate(), MessageTypeDefOf.RejectInput, historical: false);
    }
}
