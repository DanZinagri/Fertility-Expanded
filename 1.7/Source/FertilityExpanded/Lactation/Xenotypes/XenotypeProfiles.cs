using System;
using System.Collections.Generic;
using FertilityExpanded.Patches.Compatibility;
using RimWorld;
using UnityEngine;
using Verse;

namespace FertilityExpanded;

internal static class XenotypeProfiles
{
    public const float MinimumMultiplier = 0.25f;
    public const float MaximumMultiplier = 4f;
    public const float MultiplierStep = 0.05f;

    private static readonly List<XenotypeDef> xenotypes = new List<XenotypeDef>();
    private static readonly Dictionary<string, XenotypeDef> xenotypesByDefName = new Dictionary<string, XenotypeDef>();
    private static readonly List<ThingDef> milkTypes = new List<ThingDef>();
    private static readonly HashSet<string> milkTypeDefNames = new HashSet<string>();

    private static bool initialized;

    public static IReadOnlyList<XenotypeDef> Xenotypes
    {
        get
        {
            Initialize();
            return xenotypes;
        }
    }

    public static IReadOnlyList<ThingDef> MilkTypes
    {
        get
        {
            Initialize();
            return milkTypes;
        }
    }

    public static ThingDef DominantMilkDef
    {
        get
        {
            Initialize();
            return ExternalMilkCompat.DominantMilkDef ?? FE_DefOf.FE_HumanMilk;
        }
    }

    public static void Initialize()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        ExternalMilkCompat.Initialize();
        BuildXenotypeList();
        BuildMilkTypeList();
    }

    public static XenotypeDef GetXenotype(string defName)
    {
        Initialize();
        if (defName.NullOrEmpty())
        {
            return DefaultXenotype();
        }

        return xenotypesByDefName.TryGetValue(defName, out XenotypeDef xenotype)
            ? xenotype
            : DefaultXenotype();
    }

    public static LactationProfile ProfileFor(Pawn pawn)
    {
        Initialize();
        XenotypeSettings setting = SettingFor(pawn);
        ThingDef producedMilk = ResolveProducedMilkDef(setting);

        return new LactationProfile(
            producedMilk,
            setting?.storageCapacityMultiplier ?? 1f,
            setting?.productionRateMultiplier ?? 1f);
    }

    public static ThingDef ProducedMilkDef(Pawn pawn)
    {
        return ProfileFor(pawn).ProducedMilkDef;
    }

    public static ThingDef SelectedMilkDef(XenotypeSettings setting)
    {
        Initialize();
        if (setting == null || setting.producedMilkDefName.NullOrEmpty())
        {
            return DominantMilkDef;
        }

        if (!milkTypeDefNames.Contains(setting.producedMilkDefName))
        {
            return null;
        }

        return DefDatabase<ThingDef>.GetNamedSilentFail(setting.producedMilkDefName);
    }

    public static string XenotypeLabel(XenotypeDef xenotype)
    {
        return xenotype?.LabelCap.ToString() ?? "FE_NoXenotypes".Translate();
    }

    public static string MilkLabel(ThingDef milkDef)
    {
        if (milkDef == null)
        {
            return "FE_MilkDefinitionMissing".Translate();
        }

        string sourceName = milkDef.modContentPack?.Name;
        return sourceName.NullOrEmpty()
            ? milkDef.LabelCap.ToString()
            : "FE_MilkWithSource".Translate(milkDef.LabelCap.Named("MILK"), sourceName.Named("SOURCE"));
    }

    public static string SelectedMilkLabel(XenotypeSettings setting)
    {
        if (setting == null || setting.producedMilkDefName.NullOrEmpty())
        {
            return "FE_DefaultMilk".Translate(MilkLabel(DominantMilkDef).Named("MILK"));
        }

        ThingDef milkDef = SelectedMilkDef(setting);
        return milkDef != null
            ? MilkLabel(milkDef)
            : "FE_MissingSelectedMilk".Translate(setting.producedMilkDefName.Named("DEFNAME"));
    }

    private static void BuildXenotypeList()
    {
        if (!ModsConfig.BiotechActive)
        {
            return;
        }

        List<XenotypeDef> loadedXenotypes = DefDatabase<XenotypeDef>.AllDefsListForReading;
        for (int i = 0; i < loadedXenotypes.Count; i++)
        {
            XenotypeDef xenotype = loadedXenotypes[i];
            if (xenotype == null || xenotype.defName.NullOrEmpty())
            {
                continue;
            }

            xenotypes.Add(xenotype);
            xenotypesByDefName[xenotype.defName] = xenotype;
        }

        xenotypes.Sort(CompareXenotypes);
    }

    private static void BuildMilkTypeList()
    {
        ThingDef dominantMilk = ExternalMilkCompat.DominantMilkDef ?? FE_DefOf.FE_HumanMilk;
        AddMilkType(dominantMilk);

        List<ThingDef> animalMilkTypes = new List<ThingDef>();
        HashSet<string> animalMilkDefNames = new HashSet<string>();
        List<ThingDef> thingDefs = DefDatabase<ThingDef>.AllDefsListForReading;
        for (int i = 0; i < thingDefs.Count; i++)
        {
            ThingDef raceDef = thingDefs[i];
            if (raceDef?.race?.Animal != true || raceDef.comps == null)
            {
                continue;
            }

            for (int j = 0; j < raceDef.comps.Count; j++)
            {
                if (!(raceDef.comps[j] is CompProperties_Milkable milkable)
                    || !IsViableAnimalMilk(milkable.milkDef)
                    || milkTypeDefNames.Contains(milkable.milkDef.defName)
                    || !animalMilkDefNames.Add(milkable.milkDef.defName))
                {
                    continue;
                }

                animalMilkTypes.Add(milkable.milkDef);
            }
        }

        animalMilkTypes.Sort(CompareDefsByLabel);
        for (int i = 0; i < animalMilkTypes.Count; i++)
        {
            AddMilkType(animalMilkTypes[i]);
        }
    }

    private static void AddMilkType(ThingDef milkDef)
    {
        if (milkDef == null
            || milkDef.defName.NullOrEmpty()
            || !milkTypeDefNames.Add(milkDef.defName))
        {
            return;
        }

        milkTypes.Add(milkDef);
    }

    private static bool IsViableAnimalMilk(ThingDef milkDef)
    {
        return milkDef?.category == ThingCategory.Item
            && milkDef.ingestible?.CachedNutrition > 0f;
    }

    private static XenotypeSettings SettingFor(Pawn pawn)
    {
        if (!Features.AdvancedLactationSettings
            || pawn?.genes == null
            || pawn.genes.UniqueXenotype)
        {
            return null;
        }

        XenotypeDef xenotype = pawn.genes.Xenotype;
        return FertilityExpandedMod.Settings?.TryGetXenotypeLactationSetting(
            xenotype?.defName,
            out XenotypeSettings setting) == true
            ? setting
            : null;
    }

    private static ThingDef ResolveProducedMilkDef(XenotypeSettings setting)
    {
        if (setting != null
            && !setting.producedMilkDefName.NullOrEmpty()
            && milkTypeDefNames.Contains(setting.producedMilkDefName))
        {
            ThingDef selectedMilk = DefDatabase<ThingDef>.GetNamedSilentFail(setting.producedMilkDefName);
            if (selectedMilk != null)
            {
                return selectedMilk;
            }
        }

        return ExternalMilkCompat.DominantMilkDef ?? FE_DefOf.FE_HumanMilk;
    }

    private static XenotypeDef DefaultXenotype()
    {
        if (ModsConfig.BiotechActive
            && XenotypeDefOf.Baseliner != null
            && xenotypesByDefName.ContainsKey(XenotypeDefOf.Baseliner.defName))
        {
            return XenotypeDefOf.Baseliner;
        }

        return xenotypes.Count > 0 ? xenotypes[0] : null;
    }

    private static int CompareXenotypes(XenotypeDef left, XenotypeDef right)
    {
        if (left == XenotypeDefOf.Baseliner)
        {
            return right == XenotypeDefOf.Baseliner ? 0 : -1;
        }

        if (right == XenotypeDefOf.Baseliner)
        {
            return 1;
        }

        return CompareDefsByLabel(left, right);
    }

    private static int CompareDefsByLabel(Def left, Def right)
    {
        int labelComparison = string.Compare(
            left?.label,
            right?.label,
            StringComparison.CurrentCultureIgnoreCase);
        return labelComparison != 0
            ? labelComparison
            : string.Compare(left?.defName, right?.defName, StringComparison.Ordinal);
    }
}
