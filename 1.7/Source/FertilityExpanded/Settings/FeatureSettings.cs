using System.Collections.Generic;
using FertilityExpanded.Patches.Compatibility;
using RimWorld;
using Verse;

namespace FertilityExpanded;

internal static class FeatureSettings
{
    private const string LactationImplantDefName = "FE_LactationImplant";
    private const string HumanMilkDefName = "FE_HumanMilk";
    private const string InstallLactationImplantRecipeDefName = "FE_InstallLactationImplant";
    private const string TubalLigationDefName = "TubalLigation";
    private const string VasectomyDefName = "Vasectomy";
    private const string ReverseVasectomyDefName = "ReverseVasectomy";

    private static readonly HashSet<RecipeDef> lactationSystemRecipes = new HashSet<RecipeDef>();

    private static ThingDef lactationImplant;
    private static ThingDef humanMilk;
    private static HediffDef lactationImplantHediff;
    private static List<string> lactationImplantThingSetMakerTags;
    private static List<string> humanMilkThingSetMakerTags;
    private static Tradeability lactationImplantTradeability;
    private static Tradeability humanMilkTradeability;
    private static ProcedureOverrideState procedureOverrideState;
    private static bool originalStatesCaptured;

    public static void Apply()
    {
        CaptureOriginalStates();
        ApplyFertilityStatDisplay();
        procedureOverrideState.Apply(Features.ProcedureOverrides);
        ApplyLactationSystemAvailability();
        ApplyCurrentGameCycleState();
        Current.Game?.GetComponent<GameState>()?.ReconcileMenopauseForActivePawns();
    }

    public static bool IsRecipeAvailable(RecipeDef recipeDef)
    {
        CaptureOriginalStates();
        if (!RjwDrugs.IsRecipeEnabled(recipeDef))
        {
            return false;
        }

        return Features.LactationSystem
            || !lactationSystemRecipes.Contains(recipeDef);
    }

    private static void CaptureOriginalStates()
    {
        if (originalStatesCaptured)
        {
            return;
        }

        lactationImplant = DefDatabase<ThingDef>.GetNamedSilentFail(LactationImplantDefName);
        humanMilk = DefDatabase<ThingDef>.GetNamedSilentFail(HumanMilkDefName);
        lactationImplantHediff = DefDatabase<HediffDef>.GetNamedSilentFail(LactationImplantDefName);

        lactationImplantThingSetMakerTags = CopyList(lactationImplant?.thingSetMakerTags);
        humanMilkThingSetMakerTags = CopyList(humanMilk?.thingSetMakerTags);
        lactationImplantTradeability = lactationImplant?.tradeability ?? Tradeability.None;
        humanMilkTradeability = humanMilk?.tradeability ?? Tradeability.None;

        CaptureLactationSystemRecipes();
        procedureOverrideState = ProcedureOverrideState.Capture();
        originalStatesCaptured = true;
    }

    private static void CaptureLactationSystemRecipes()
    {
        List<RecipeDef> recipes = DefDatabase<RecipeDef>.AllDefsListForReading;
        for (int i = 0; i < recipes.Count; i++)
        {
            RecipeDef recipe = recipes[i];
            if (IsLactationSystemRecipe(recipe))
            {
                lactationSystemRecipes.Add(recipe);
            }
        }
    }

    private static void ApplyFertilityStatDisplay()
    {
        if (StatDefOf.Fertility != null)
        {
            StatDefOf.Fertility.showOnDefaultValue = true;
        }
    }

    private static void ApplyLactationSystemAvailability()
    {
        bool enabled = Features.LactationSystem;

        if (lactationImplant != null)
        {
            lactationImplant.tradeability = enabled
                ? lactationImplantTradeability
                : Tradeability.None;
            lactationImplant.thingSetMakerTags = enabled
                ? CopyList(lactationImplantThingSetMakerTags)
                : new List<string>();
        }

        if (humanMilk != null)
        {
            humanMilk.tradeability = enabled
                ? humanMilkTradeability
                : Tradeability.None;
            humanMilk.thingSetMakerTags = enabled
                ? CopyList(humanMilkThingSetMakerTags)
                : new List<string>();
        }
    }

    private static void ApplyCurrentGameCycleState()
    {
        if (Current.Game == null)
        {
            return;
        }

        if (Features.FertilityCycles)
        {
            List<Map> maps = Find.Maps;
            for (int mapIndex = 0; mapIndex < maps.Count; mapIndex++)
            {
                List<Pawn> mapPawns = maps[mapIndex].mapPawns.AllHumanlikeSpawned;
                for (int pawnIndex = mapPawns.Count - 1; pawnIndex >= 0; pawnIndex--)
                {
                    FertilityCycles.EnsureCycle(mapPawns[pawnIndex]);
                }
            }

            return;
        }

        List<Pawn> pawns = PawnsFinder.All_AliveOrDead;
        for (int i = 0; i < pawns.Count; i++)
        {
            Pawn pawn = pawns[i];
            FertilityCycles.RemoveCycle(pawn);
            Menopause.RemoveTransition(pawn);
        }
    }

    private static bool IsLactationSystemRecipe(RecipeDef recipeDef)
    {
        if (recipeDef == null)
        {
            return false;
        }

        if (recipeDef.defName == InstallLactationImplantRecipeDefName
            || recipeDef.addsHediff == lactationImplantHediff)
        {
            return true;
        }

        List<ThingDefCountClass> products = recipeDef.products;
        if (products == null)
        {
            return false;
        }

        for (int i = 0; i < products.Count; i++)
        {
            ThingDef product = products[i]?.thingDef;
            if (product == lactationImplant || product == humanMilk)
            {
                return true;
            }
        }

        return false;
    }

    private static List<T> CopyList<T>(List<T> source)
    {
        return source == null ? null : new List<T>(source);
    }

    private sealed class ProcedureOverrideState
    {
        private readonly RecipeDef tubalLigationRecipe;
        private readonly RecipeDef vasectomyRecipe;
        private readonly RecipeDef reverseVasectomyRecipe;
        private readonly HediffDef tubalLigationHediff;
        private readonly HediffDef vasectomyHediff;
        private readonly string tubalLigationLabel;
        private readonly string tubalLigationJobString;
        private readonly string tubalLigationSuccessMessage;
        private readonly string tubalLigationHediffLabel;
        private readonly string tubalLigationHediffDescription;
        private readonly string vasectomyDescription;
        private readonly HediffDef vasectomyFailureHediff;
        private readonly string vasectomyHediffDescription;
        private readonly string reverseVasectomyDescription;
        private readonly HediffDef reverseVasectomyFailureHediff;

        private ProcedureOverrideState(
            RecipeDef tubalLigationRecipe,
            RecipeDef vasectomyRecipe,
            RecipeDef reverseVasectomyRecipe,
            HediffDef tubalLigationHediff,
            HediffDef vasectomyHediff)
        {
            this.tubalLigationRecipe = tubalLigationRecipe;
            this.vasectomyRecipe = vasectomyRecipe;
            this.reverseVasectomyRecipe = reverseVasectomyRecipe;
            this.tubalLigationHediff = tubalLigationHediff;
            this.vasectomyHediff = vasectomyHediff;

            tubalLigationLabel = tubalLigationRecipe?.label;
            tubalLigationJobString = tubalLigationRecipe?.jobString;
            tubalLigationSuccessMessage = tubalLigationRecipe?.successfullyRemovedHediffMessage;
            tubalLigationHediffLabel = tubalLigationHediff?.label;
            tubalLigationHediffDescription = tubalLigationHediff?.description;
            vasectomyDescription = vasectomyRecipe?.description;
            vasectomyFailureHediff = vasectomyRecipe?.addsHediffOnFailure;
            vasectomyHediffDescription = vasectomyHediff?.description;
            reverseVasectomyDescription = reverseVasectomyRecipe?.description;
            reverseVasectomyFailureHediff = reverseVasectomyRecipe?.addsHediffOnFailure;
        }

        public static ProcedureOverrideState Capture()
        {
            return new ProcedureOverrideState(
                DefDatabase<RecipeDef>.GetNamedSilentFail(TubalLigationDefName),
                DefDatabase<RecipeDef>.GetNamedSilentFail(VasectomyDefName),
                DefDatabase<RecipeDef>.GetNamedSilentFail(ReverseVasectomyDefName),
                DefDatabase<HediffDef>.GetNamedSilentFail(TubalLigationDefName),
                DefDatabase<HediffDef>.GetNamedSilentFail(VasectomyDefName));
        }

        public void Apply(bool enabled)
        {
            if (tubalLigationRecipe != null)
            {
                tubalLigationRecipe.label = enabled ? "FE_TubalLigationLabel".Translate() : tubalLigationLabel;
                tubalLigationRecipe.jobString = enabled ? "FE_TubalLigationJob".Translate() : tubalLigationJobString;
                tubalLigationRecipe.successfullyRemovedHediffMessage = enabled
                    ? "FE_TubalLigationSuccess".Translate()
                    : tubalLigationSuccessMessage;
                tubalLigationRecipe.ClearCachedData();
            }

            if (tubalLigationHediff != null)
            {
                tubalLigationHediff.label = enabled ? "FE_TubalLigationHediff".Translate() : tubalLigationHediffLabel;
                tubalLigationHediff.description = enabled
                    ? "FE_TubalLigationDescription".Translate()
                    : tubalLigationHediffDescription;
                tubalLigationHediff.ClearCachedData();
            }

            if (vasectomyRecipe != null)
            {
                vasectomyRecipe.description = enabled
                    ? "FE_VasectomyDescription".Translate()
                    : vasectomyDescription;
                vasectomyRecipe.addsHediffOnFailure = enabled ? null : vasectomyFailureHediff;
                vasectomyRecipe.ClearCachedData();
            }

            if (vasectomyHediff != null)
            {
                vasectomyHediff.description = enabled
                    ? "FE_VasectomyHediffDescription".Translate()
                    : vasectomyHediffDescription;
                vasectomyHediff.ClearCachedData();
            }

            if (reverseVasectomyRecipe != null)
            {
                reverseVasectomyRecipe.description = enabled
                    ? "FE_ReverseVasectomyDescription".Translate()
                    : reverseVasectomyDescription;
                reverseVasectomyRecipe.addsHediffOnFailure = enabled ? null : reverseVasectomyFailureHediff;
                reverseVasectomyRecipe.ClearCachedData();
            }
        }
    }
}
