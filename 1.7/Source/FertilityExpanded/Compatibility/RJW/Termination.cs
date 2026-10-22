using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FertilityExpanded.Patches.Compatibility;

internal static class RjwTerminationPatch
{
    private const string MechPregnancyDefName = "RJW_pregnancy_mech";
    private const string AbortionWorkerTypeName = "rjw.Recipe_Abortion";
    private const int ForbiddenTerminationGoodwillImpact = -35;

    public static void Prefix(RecipeWorker __instance, Pawn pawn, out int __state)
    {
        HediffDef pregnancyDef = EligiblePregnancyDef(__instance);
        __state = CountHediffs(pawn, pregnancyDef);
    }

    public static void Postfix(RecipeWorker __instance, Pawn pawn, Pawn billDoer, int __state)
    {
        if (__state <= 0)
        {
            return;
        }

        HediffDef pregnancyDef = EligiblePregnancyDef(__instance);
        if (CountHediffs(pawn, pregnancyDef) >= __state)
        {
            return;
        }

        TerminationBeliefs.ReportViolationIfForbidden(
            pawn,
            billDoer,
            pawn?.HomeFaction,
            ForbiddenTerminationGoodwillImpact);
    }

    public static void IsViolationPostfix(
        RecipeWorker __instance,
        Pawn pawn,
        Faction billDoerFaction,
        ref bool __result)
    {
        if (EligiblePregnancyDef(__instance) != null)
        {
            __result = TerminationBeliefs.IsViolation(pawn, billDoerFaction);
        }
    }

    private static HediffDef EligiblePregnancyDef(RecipeWorker recipeWorker)
    {
        if (recipeWorker?.GetType().FullName != AbortionWorkerTypeName)
        {
            return null;
        }

        HediffDef pregnancyDef = recipeWorker?.recipe?.removesHediff;
        if (pregnancyDef == null
            || !pregnancyDef.pregnant
            || pregnancyDef.defName == MechPregnancyDefName)
        {
            return null;
        }

        return pregnancyDef;
    }

    private static int CountHediffs(Pawn pawn, HediffDef hediffDef)
    {
        if (pawn?.health?.hediffSet == null || hediffDef == null)
        {
            return 0;
        }

        int count = 0;
        List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
        for (int i = 0; i < hediffs.Count; i++)
        {
            if (hediffs[i].def == hediffDef)
            {
                count++;
            }
        }

        return count;
    }
}
