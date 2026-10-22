using RimWorld;
using Verse;

namespace FertilityExpanded.Patches.Compatibility;

internal static class IntimateEvents
{
    public static void NotifyIntimateMoment(Pawn pawn, Pawn partner)
    {
        if (!ModsConfig.BiotechActive)
        {
            return;
        }

        SyncPawnAfterIntimateMoment(pawn);
        SyncPawnAfterIntimateMoment(partner);
    }

    public static void NotifyMemoryGained(Pawn pawn, ThoughtDef thoughtDef, Pawn otherPawn)
    {
        if (thoughtDef == null)
        {
            return;
        }

        if (thoughtDef == ThoughtDefOf.GotSomeLovin || thoughtDef.defName == "SP_PassionateLovin")
        {
            NotifyIntimateMoment(pawn, otherPawn);
        }
    }

    public static void SyncRjwPawns(object props)
    {
        if (props == null)
        {
            return;
        }

        ModDetection.TryReadPawnField(props, "pawn", out Pawn pawn);
        ModDetection.TryReadPawnField(props, "partner", out Pawn partner);
        NotifyIntimateMoment(pawn, partner);
    }

    private static void SyncPawnAfterIntimateMoment(Pawn pawn)
    {
        if (pawn?.health == null)
        {
            return;
        }

        if (ReproductionCompat.HasCycleBlockingHediff(pawn))
        {
            FertilityCycles.RemoveCycle(pawn);
            if (ReproductionCompat.HasMenopauseBlockingHediff(pawn))
            {
                Menopause.RemoveTransition(pawn);
            }
        }
        else
        {
            FertilityCycles.EnsureCycle(pawn);
        }
    }
}
