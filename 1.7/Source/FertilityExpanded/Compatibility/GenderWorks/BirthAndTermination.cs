using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FertilityExpanded.Patches.Compatibility;

internal static class GenderWorksCompat
{
    private const string EggTerminationRecipeDefName = "SEX_TerminateEggGestation";
    private const int ForbiddenTerminationGoodwillImpact = -35;

    [ThreadStatic]
    private static int eggLayingDepth;

    public static bool EggLayingInProgress => eggLayingDepth > 0;

    public static bool FertilityExpandedHandlesEggRecovery
    {
        get
        {
            if (!Features.PostpartumRecovery)
            {
                return false;
            }

            EggPostpartumMode mode = FertilityExpandedMod.Settings?.genderWorksEggPostpartumMode
                ?? EggPostpartumMode.FertilityExpanded;
            return mode == EggPostpartumMode.FertilityExpanded;
        }
    }

    public static void EggLaidPrefix()
    {
        eggLayingDepth++;
    }

    public static void EggLaidPostfix(Hediff __instance)
    {
        Pawn mother = __instance?.pawn;
        if (mother?.health == null || mother.health.hediffSet.hediffs.Contains(__instance))
        {
            return;
        }

        Current.Game?.GetComponent<GameState>()?.RegisterPostpartumCareWindow(mother);

        if (FertilityExpandedHandlesEggRecovery)
        {
            FertilityCycles.AddPostpartumRecovery(mother);
        }
    }

    public static Exception EggLaidFinalizer(Exception __exception)
    {
        if (eggLayingDepth > 0)
        {
            eggLayingDepth--;
        }

        return __exception;
    }

    public static void HatchPrefix(Map spawnMap, out HashSet<Pawn> __state)
    {
        __state = spawnMap == null
            ? null
            : new HashSet<Pawn>(spawnMap.mapPawns.AllPawnsSpawned);
    }

    public static void HatchPostfix(Map spawnMap, Pawn ___mother, HashSet<Pawn> __state)
    {
        if (spawnMap == null || ___mother == null || __state == null)
        {
            return;
        }

        Pawn firstBaby = null;
        List<Pawn> newborns = new List<Pawn>();
        IReadOnlyList<Pawn> pawns = spawnMap.mapPawns.AllPawnsSpawned;
        for (int i = 0; i < pawns.Count; i++)
        {
            Pawn baby = pawns[i];
            if (__state.Contains(baby)
                || baby == null
                || baby.Dead
                || baby.relations == null
                || !baby.relations.DirectRelationExists(PawnRelationDefOf.Parent, ___mother))
            {
                continue;
            }

            Current.Game?.GetComponent<GameState>()?.RegisterPostpartumNewborn(___mother, baby);
            firstBaby ??= baby;
            newborns.Add(baby);
        }

        if (firstBaby != null)
        {
            BirthObligations.TryRegisterNewLifeRitual(___mother, newborns);
            BirthEffects.ApplyBabyBond(___mother, firstBaby);
            BirthEffects.ApplyFamilyEffects(___mother, firstBaby);
        }
    }

    public static void IsViolationPostfix(
        RecipeWorker __instance,
        Pawn pawn,
        Faction billDoerFaction,
        ref bool __result)
    {
        if (IsEggTerminationRecipe(__instance))
        {
            __result = TerminationBeliefs.IsViolation(pawn, billDoerFaction);
        }
    }

    public static void ReportViolationPrefix(RecipeWorker __instance, ref int goodwillImpact)
    {
        if (IsEggTerminationRecipe(__instance))
        {
            goodwillImpact = ForbiddenTerminationGoodwillImpact;
        }
    }

    private static bool IsEggTerminationRecipe(RecipeWorker recipeWorker)
    {
        return recipeWorker?.recipe?.defName == EggTerminationRecipeDefName;
    }
}
