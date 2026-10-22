using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace FertilityExpanded.Patches.Compatibility;

internal static class RjwDrugs
{
    private static readonly string[] ReproductiveDrugDefNames =
    {
        "FE_BirthControlPill",
        "FE_HerbalFertilityTonic",
        "FE_HerbalContraceptiveTea",
        "FE_FertilityTreatment"
    };

    private static readonly Dictionary<ThingDef, ThingState> thingStates = new Dictionary<ThingDef, ThingState>();
    private static readonly Dictionary<RecipeDef, RecipeState> recipeStates = new Dictionary<RecipeDef, RecipeState>();
    private static readonly FieldInfo DrugPolicyEntriesField = AccessTools.Field(typeof(DrugPolicy), "entriesInt");

    private static bool originalsCaptured;
    private static bool sessionInitialized;
    private static bool sessionDrugsDisabled;
    private static RjwDrugMode sessionMode = RjwDrugMode.Auto;
    private static Game synchronizedGame;

    public static bool FertilityExpandedDrugsDisabled
    {
        get
        {
            InitializeSession();
            return sessionDrugsDisabled;
        }
    }

    public static string CurrentStatusLabel
    {
        get
        {
            InitializeSession();
            if (!ModDetection.RimJobWorldActive)
            {
                return "FE_RjwInactiveStatus".Translate();
            }

            string status = sessionDrugsDisabled
                ? "FE_RjwDrugsDisabledStatus".Translate()
                : "FE_RjwDrugsEnabledStatus".Translate();
            RjwDrugMode selectedMode = FertilityExpandedMod.Settings?.rjwDrugCompatibilityMode
                ?? RjwDrugMode.Auto;
            bool restartPending = selectedMode != sessionMode
                || ResolveFertilityExpandedDrugsDisabled(selectedMode) != sessionDrugsDisabled;
            return restartPending
                ? "FE_RjwOwnershipPending".Translate(status.Named("STATUS"))
                : "FE_RjwOwnershipRestart".Translate(status.Named("STATUS"));
        }
    }

    public static void InitializeSession()
    {
        if (sessionInitialized)
        {
            return;
        }

        CaptureOriginalState();
        sessionMode = FertilityExpandedMod.Settings?.rjwDrugCompatibilityMode ?? RjwDrugMode.Auto;
        sessionDrugsDisabled = ResolveFertilityExpandedDrugsDisabled(sessionMode);
        int visibilityMask = DisabledDrugMask(sessionDrugsDisabled);
        ApplyThingDefVisibility(visibilityMask);
        ApplyRecipeVisibility(visibilityMask);
        sessionInitialized = true;
    }

    public static void SynchronizeLoadedDrugPolicies()
    {
        InitializeSession();
        Game game = Current.Game;
        if (game == null || game == synchronizedGame)
        {
            return;
        }

        EnsureLoadedDrugPolicyEntries();
        synchronizedGame = game;
    }

    public static bool IsDrugEnabled(ThingDef drug)
    {
        return drug == null
            || DrugIndex(drug) < 0
            || !FertilityExpandedDrugsDisabled;
    }

    public static bool IsRecipeEnabled(RecipeDef recipeDef)
    {
        CaptureOriginalState();
        return recipeDef == null
            || !recipeStates.TryGetValue(recipeDef, out RecipeState state)
            || IsDrugEnabled(state.Drug);
    }

    private static bool ResolveFertilityExpandedDrugsDisabled(RjwDrugMode mode)
    {
        if (!ModDetection.RimJobWorldActive)
        {
            return false;
        }

        switch (mode)
        {
            case RjwDrugMode.FertilityExpanded:
                return false;
            case RjwDrugMode.RimJobWorld:
                return true;
            default:
                if (ModDetection.RegularRimJobWorldActive)
                {
                    return !ModDetection.RimJobWorldUsesVanillaPregnancy;
                }

                return false;
        }
    }

    private static void CaptureOriginalState()
    {
        if (originalsCaptured)
        {
            return;
        }

        List<ThingDef> reproductiveDrugs = ReproductiveDrugDefs();
        HashSet<ThingDef> reproductiveDrugSet = new HashSet<ThingDef>(reproductiveDrugs);

        for (int i = 0; i < reproductiveDrugs.Count; i++)
        {
            ThingDef thingDef = reproductiveDrugs[i];
            if (thingDef?.ingestible == null || thingStates.ContainsKey(thingDef))
            {
                continue;
            }

            thingStates.Add(thingDef, new ThingState(
                thingDef.ingestible.drugCategory,
                CopyList(thingDef.recipeMaker?.recipeUsers),
                thingDef.tradeability,
                CopyList(thingDef.thingSetMakerTags)));
        }

        List<RecipeDef> recipeDefs = DefDatabase<RecipeDef>.AllDefsListForReading;
        for (int i = 0; i < recipeDefs.Count; i++)
        {
            RecipeDef recipeDef = recipeDefs[i];
            ThingDef drug = ReproductiveDrugForRecipe(recipeDef, reproductiveDrugSet);
            if (drug != null && !recipeStates.ContainsKey(recipeDef))
            {
                recipeStates.Add(recipeDef, new RecipeState(CopyList(recipeDef.recipeUsers), drug));
            }
        }

        originalsCaptured = true;
    }

    private static void ApplyThingDefVisibility(int visibilityMask)
    {
        foreach (KeyValuePair<ThingDef, ThingState> state in thingStates)
        {
            ThingDef thingDef = state.Key;
            if (thingDef?.ingestible == null)
            {
                continue;
            }

            bool disabled = IsDisabled(thingDef, visibilityMask);
            thingDef.ingestible.drugCategory = disabled ? DrugCategory.None : state.Value.DrugCategory;
            thingDef.tradeability = disabled ? Tradeability.None : state.Value.Tradeability;
            thingDef.thingSetMakerTags = disabled
                ? new List<string>()
                : CopyList(state.Value.ThingSetMakerTags);
            if (thingDef.recipeMaker != null)
            {
                thingDef.recipeMaker.recipeUsers = disabled ? new List<ThingDef>() : CopyList(state.Value.RecipeMakerUsers);
            }
        }
    }

    private static void ApplyRecipeVisibility(int visibilityMask)
    {
        foreach (KeyValuePair<RecipeDef, RecipeState> state in recipeStates)
        {
            RecipeDef recipeDef = state.Key;
            if (recipeDef != null)
            {
                recipeDef.recipeUsers = IsDisabled(state.Value.Drug, visibilityMask)
                    ? new List<ThingDef>()
                    : CopyList(state.Value.RecipeUsers);
            }
        }
    }

    private static int DisabledDrugMask(bool compatibilityDisabled)
    {
        int mask = 0;
        for (int i = 0; i < ReproductiveDrugDefNames.Length; i++)
        {
            ThingDef drug = DefDatabase<ThingDef>.GetNamedSilentFail(ReproductiveDrugDefNames[i]);
            if (drug != null && compatibilityDisabled)
            {
                mask |= 1 << i;
            }
        }

        return mask;
    }

    private static bool IsDisabled(ThingDef drug, int visibilityMask)
    {
        int index = DrugIndex(drug);
        return index >= 0 && (visibilityMask & (1 << index)) != 0;
    }

    private static int DrugIndex(ThingDef drug)
    {
        if (drug == null)
        {
            return -1;
        }

        for (int i = 0; i < ReproductiveDrugDefNames.Length; i++)
        {
            if (drug.defName == ReproductiveDrugDefNames[i])
            {
                return i;
            }
        }

        return -1;
    }

    private static void EnsureLoadedDrugPolicyEntries()
    {
        if (Current.Game?.drugPolicyDatabase?.AllPolicies == null || DrugPolicyEntriesField == null)
        {
            return;
        }

        List<ThingDef> reproductiveDrugs = ReproductiveDrugDefs();
        foreach (DrugPolicy policy in Current.Game.drugPolicyDatabase.AllPolicies)
        {
            List<DrugPolicyEntry> entries = DrugPolicyEntriesField.GetValue(policy) as List<DrugPolicyEntry>;
            if (entries == null)
            {
                continue;
            }

            bool addedEntry = false;
            for (int i = 0; i < reproductiveDrugs.Count; i++)
            {
                ThingDef drug = reproductiveDrugs[i];
                if (drug != null && !HasPolicyEntry(entries, drug))
                {
                    entries.Add(new DrugPolicyEntry
                    {
                        drug = drug,
                        allowedForAddiction = true
                    });
                    addedEntry = true;
                }
            }

            if (addedEntry)
            {
                entries.Sort((left, right) => DrugListOrder(left?.drug).CompareTo(DrugListOrder(right?.drug)));
            }
        }
    }

    private static bool HasPolicyEntry(List<DrugPolicyEntry> entries, ThingDef drug)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i]?.drug == drug)
            {
                return true;
            }
        }

        return false;
    }

    private static float DrugListOrder(ThingDef drug)
    {
        return drug?.GetCompProperties<CompProperties_Drug>()?.listOrder ?? 0f;
    }

    private static List<ThingDef> ReproductiveDrugDefs()
    {
        List<ThingDef> drugs = new List<ThingDef>();
        for (int i = 0; i < ReproductiveDrugDefNames.Length; i++)
        {
            ThingDef drug = DefDatabase<ThingDef>.GetNamedSilentFail(ReproductiveDrugDefNames[i]);
            if (drug != null)
            {
                drugs.Add(drug);
            }
        }

        return drugs;
    }

    private static ThingDef ReproductiveDrugForRecipe(RecipeDef recipeDef, HashSet<ThingDef> reproductiveDrugs)
    {
        if (recipeDef == null)
        {
            return null;
        }

        List<ThingDefCountClass> products = recipeDef.products;
        if (products != null)
        {
            for (int i = 0; i < products.Count; i++)
            {
                if (reproductiveDrugs.Contains(products[i]?.thingDef))
                {
                    return products[i].thingDef;
                }
            }
        }

        if (recipeDef.workerClass != typeof(Recipe_AdministerIngestible)
            || recipeDef.fixedIngredientFilter == null)
        {
            return null;
        }

        foreach (ThingDef reproductiveDrug in reproductiveDrugs)
        {
            if (recipeDef.fixedIngredientFilter.Allows(reproductiveDrug))
            {
                return reproductiveDrug;
            }
        }

        return null;
    }

    private static List<T> CopyList<T>(List<T> source)
    {
        return source == null ? null : new List<T>(source);
    }

    private sealed class ThingState
    {
        public readonly DrugCategory DrugCategory;
        public readonly List<ThingDef> RecipeMakerUsers;
        public readonly Tradeability Tradeability;
        public readonly List<string> ThingSetMakerTags;

        public ThingState(
            DrugCategory drugCategory,
            List<ThingDef> recipeMakerUsers,
            Tradeability tradeability,
            List<string> thingSetMakerTags)
        {
            DrugCategory = drugCategory;
            RecipeMakerUsers = recipeMakerUsers;
            Tradeability = tradeability;
            ThingSetMakerTags = thingSetMakerTags;
        }
    }

    private sealed class RecipeState
    {
        public readonly List<ThingDef> RecipeUsers;
        public readonly ThingDef Drug;

        public RecipeState(List<ThingDef> recipeUsers, ThingDef drug)
        {
            RecipeUsers = recipeUsers;
            Drug = drug;
        }
    }
}
