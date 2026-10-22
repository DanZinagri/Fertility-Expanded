using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FertilityExpanded;

public class FosterResolutionLetter : ChoiceLetter
{
    private Pawn child;
    private Faction sourceFaction;

    public override bool CanShowInLetterStack => base.CanShowInLetterStack
        && child != null
        && !child.Destroyed
        && !child.Dead
        && Current.Game?.GetComponent<GameState>()?.HasActiveFosterPlacement(child) == true;

    public override IEnumerable<DiaOption> Choices
    {
        get
        {
            if (!base.ArchivedOnly)
            {
                bool canResolve = FosterResolution.TryGetResolvablePlacement(child, out FosterPlacementRecord placement);
                bool expired = placement != null && placement.reviewDeadlineTick >= 0
                    && Find.TickManager.TicksGame >= placement.reviewDeadlineTick;
                DiaOption returnHome = new DiaOption("FE_ReturnFosterChild".Translate())
                {
                    action = delegate
                    {
                        if (FosterResolution.TryReturnHome(child, sourceFaction))
                        {
                            Find.LetterStack.RemoveLetter(this);
                        }
                    },
                    resolveTree = true
                };

                if (!canResolve || expired)
                {
                    returnHome.Disable((expired ? "FE_FosterReturnExpired" : "FE_FosterNeedsHome").Translate());
                }
                else if (placement.sourceFaction == null && sourceFaction == null)
                {
                    returnHome.Disable("FE_FosterSourceMissing".Translate());
                }
                yield return returnHome;

                DiaOption adopt = new DiaOption("FE_AdoptFosterChild".Translate())
                {
                    action = delegate
                    {
                        if (FosterResolution.TryAdoptFosterChild(child, sourceFaction))
                        {
                            Find.LetterStack.RemoveLetter(this);
                        }
                    },
                    resolveTree = true
                };
                if (!canResolve)
                {
                    adopt.Disable("FE_FosterNeedsHome".Translate());
                }
                yield return adopt;
            }

            if (base.ArchivedOnly)
            {
                yield return base.Option_Close;
            }
            else
            {
                yield return base.Option_Postpone;
            }
        }
    }

    public void Configure(Pawn child, Faction sourceFaction)
    {
        this.child = child;
        this.sourceFaction = sourceFaction;
        title = "FE_FosterResolutionTitle".Translate();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref child, "child", saveDestroyedThings: true);
        Scribe_References.Look(ref sourceFaction, "sourceFaction");
    }
}
