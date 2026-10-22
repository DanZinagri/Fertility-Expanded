using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FertilityExpanded;

public class ParentAssignmentLetter : ChoiceLetter
{
    private Pawn child;
    private ParentKind assignmentKind;
    private ParentReason assignmentReason;

    public override bool CanShowInLetterStack => base.CanShowInLetterStack
        && child != null
        && !child.Destroyed
        && !child.Dead
        && child.Faction == Faction.OfPlayer
        && child.relations != null
        && (assignmentKind != ParentKind.Foster
            || Current.Game?.GetComponent<GameState>()?.HasActiveFosterPlacement(child) == true)
        && !ParentAssignment.HasBlockingParentAssignment(
            child,
            assignmentKind == ParentKind.Foster ? FE_DefOf.FE_FosterParent : FE_DefOf.FE_AdoptiveParent,
            assignmentKind);

    public override IEnumerable<DiaOption> Choices
    {
        get
        {
            if (!base.ArchivedOnly)
            {
                foreach (Pawn candidate in ParentAssignment.ValidParentCandidates())
                {
                    if (candidate == child)
                    {
                        continue;
                    }

                    Pawn parent = candidate;
                    yield return new DiaOption(parent.LabelShortCap)
                    {
                        action = delegate
                        {
                            if (ParentAssignment.TryAssignParent(child, parent, assignmentKind))
                            {
                                Find.LetterStack.RemoveLetter(this);
                            }
                        },
                        resolveTree = true
                    };
                }
            }

            if (base.ArchivedOnly)
            {
                yield return base.Option_JumpToLocation;
                yield return base.Option_Close;
            }
            else
            {
                yield return base.Option_JumpToLocationAndPostpone;
                yield return base.Option_Postpone;
            }
        }
    }

    public void Configure(Pawn child, ParentKind assignmentKind, ParentReason assignmentReason = ParentReason.Placement)
    {
        this.child = child;
        this.assignmentKind = assignmentKind;
        this.assignmentReason = assignmentReason;
        title = TitleFor(assignmentKind, assignmentReason);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref child, "child", saveDestroyedThings: true);
        Scribe_Values.Look(ref assignmentKind, "assignmentKind", ParentKind.Adoptive);
        Scribe_Values.Look(ref assignmentReason, "assignmentReason", ParentReason.Placement);
    }

    private static TaggedString TitleFor(ParentKind assignmentKind, ParentReason assignmentReason)
    {
        if (assignmentKind == ParentKind.Foster)
        {
            return "FE_AssignFosterParentTitle".Translate();
        }

        return assignmentReason == ParentReason.Orphaned
            ? "FE_OrphanAdoptionTitle".Translate()
            : "FE_AssignAdoptiveParentTitle".Translate();
    }
}
