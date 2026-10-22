using System.Collections.Generic;
using FertilityExpanded.Patches.Compatibility;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace FertilityExpanded.Patches;

[HarmonyPatch(typeof(PregnancyUtility), nameof(PregnancyUtility.ApplyBirthOutcome))]
public static class BirthOutcomePatch
{
    private static bool applyingExtraBirthOutcome;

    internal static bool ApplyingExtraBirthOutcome => applyingExtraBirthOutcome;

    public static void Prefix(
        RitualOutcomePossibility outcome,
        float quality,
        Precept_Ritual ritual,
        List<GeneDef> genes,
        Pawn geneticMother,
        Thing birtherThing,
        Pawn father,
        Pawn doctor,
        LordJob_Ritual lordJobRitual,
        RitualRoleAssignments assignments,
        bool preventLetter,
        out List<Pawn> __state)
    {
        __state = null;
        Pawn birther = birtherThing as Pawn;
        if (applyingExtraBirthOutcome || birther?.health == null || birther.Dead)
        {
            return;
        }

        Current.Game?.GetComponent<GameState>()?.RegisterPostpartumCareWindow(birther);
        __state = new List<Pawn>(3);
        ApplyExtraBirthOutcomes(outcome, quality, ritual, genes, geneticMother, birtherThing, father, doctor, lordJobRitual, assignments, preventLetter, __state);
    }

    public static void Postfix(Thing __result, Thing birtherThing, List<Pawn> __state)
    {
        Pawn birther = birtherThing as Pawn;
        if (__state?.Count > 0)
        {
            MultipleBirths.RemoveDuplicateBirthRecoveryHediffs(birther);
        }

        Pawn baby = __result as Pawn;
        if (__state != null)
        {
            if (baby != null && !baby.Dead)
            {
                __state.Add(baby);
            }
            BirthObligations.TryRegisterNewLifeRitual(birther, __state);
        }

        if (birther?.health == null || birther.Dead)
        {
            return;
        }

        if (!applyingExtraBirthOutcome)
        {
            BirthEffects.ApplyPostpartumState(
                birther,
                IntimacyRecovery.FertilityExpandedHandlesRecovery);
        }

        if (baby != null && !baby.Dead)
        {
            Current.Game?.GetComponent<GameState>()?.RegisterPostpartumNewborn(birther, baby);
            BirthEffects.ApplyBabyBond(birther, baby);
            if (!applyingExtraBirthOutcome)
            {
                BirthEffects.ApplyFamilyEffects(birther, baby);
            }
        }
    }

    private static int ApplyExtraBirthOutcomes(
        RitualOutcomePossibility outcome,
        float quality,
        Precept_Ritual ritual,
        List<GeneDef> genes,
        Pawn geneticMother,
        Thing birtherThing,
        Pawn father,
        Pawn doctor,
        LordJob_Ritual lordJobRitual,
        RitualRoleAssignments assignments,
        bool preventLetter,
        List<Pawn> newborns)
    {
        Pawn birther = birtherThing as Pawn;
        int extraBirths = MultipleBirths.ConsumeExtraBirths(birther);
        if (extraBirths <= 0)
        {
            return 0;
        }

        applyingExtraBirthOutcome = true;
        try
        {
            for (int i = 0; i < extraBirths; i++)
            {
                if (birther.Dead)
                {
                    break;
                }

                Pawn baby = PregnancyUtility.ApplyBirthOutcome(outcome, quality, ritual, genes, geneticMother, birtherThing, father, doctor, lordJobRitual, assignments, preventLetter) as Pawn;
                if (baby != null && !baby.Dead)
                {
                    newborns.Add(baby);
                }
            }
        }
        finally
        {
            applyingExtraBirthOutcome = false;
            MultipleBirths.RemoveDuplicateBirthRecoveryHediffs(birther);
        }

        return extraBirths;
    }
}
