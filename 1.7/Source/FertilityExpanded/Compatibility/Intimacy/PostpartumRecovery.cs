using Verse;

namespace FertilityExpanded.Patches.Compatibility;

internal static class IntimacyRecovery
{
    private const string IntimacyRecoveryDefName = "SEX_PostBirth";

    private static HediffDef intimacyRecovery;

    public static void Initialize()
    {
        if (ModDetection.IntimacyActive)
        {
            intimacyRecovery = DefDatabase<HediffDef>.GetNamedSilentFail(IntimacyRecoveryDefName);
        }
    }

    public static bool FertilityExpandedHandlesRecovery
    {
        get
        {
            if (!Features.PostpartumRecovery)
            {
                return false;
            }

            if (!ModDetection.IntimacyActive)
            {
                return true;
            }

            IntimacyPostpartumMode mode = FertilityExpandedMod.Settings?.intimacyPostpartumMode
                ?? IntimacyPostpartumMode.FertilityExpanded;
            return mode == IntimacyPostpartumMode.FertilityExpanded;
        }
    }

    public static bool ApplyIntimacyRecoveryPrefix()
    {
        if (GenderWorksCompat.EggLayingInProgress)
        {
            return !GenderWorksCompat.FertilityExpandedHandlesEggRecovery;
        }

        return !FertilityExpandedHandlesRecovery;
    }

    public static bool IsPostpartumRecovery(HediffDef hediffDef)
    {
        return hediffDef != null
            && (hediffDef == FE_DefOf.FE_PostpartumRecovery || hediffDef == intimacyRecovery);
    }

    public static Hediff GetActiveRecoveryHediff(Pawn pawn)
    {
        if (pawn?.health?.hediffSet == null)
        {
            return null;
        }

        Hediff recovery = pawn.health.hediffSet.GetFirstHediffOfDef(FE_DefOf.FE_PostpartumRecovery);
        if (recovery != null || intimacyRecovery == null)
        {
            return recovery;
        }

        return pawn.health.hediffSet.GetFirstHediffOfDef(intimacyRecovery);
    }
}
