using System.Collections.Generic;
using FertilityExpanded.Patches.Compatibility;
using UnityEngine;
using Verse;

namespace FertilityExpanded;

public class FertilityExpandedSettings : ModSettings
{
    private const int CurrentSettingsVersion = 2;
    private const float DefaultMultibirthGeneCommonality = 0.05f;

    private int settingsVersion = CurrentSettingsVersion;

    public bool fertilityCycles = true;
    public bool menopause = true;
    public bool multibirthRebalance = true;
    public float multibirthGeneCommonality = DefaultMultibirthGeneCommonality;

    public bool postpartumRecovery = true;
    public bool postpartumExhaustionOverride = true;
    public bool postpartumSadness = true;

    public bool procedureOverrides = true;
    public bool lactationSystem = true;
    public bool advancedLactationSettings;
    public bool maleLactationFromGene;
    public bool maleLactationFromRole = true;
    public bool maleLactationFromImplant = true;

    private List<XenotypeSettings> xenotypeLactationSettings = new List<XenotypeSettings>();
    private Dictionary<string, XenotypeSettings> xenotypeLactationSettingsByDefName;

    public bool disableAdoptionQuests;
    public bool disableFosterQuests;

    public bool babyBondingMemories = true;
    public bool supportedPostpartumCareMemory = true;
    public bool neglectedPostpartumCareMemory = true;
    public bool singleParentStrain = true;
    public bool coparentSupport = true;
    public bool childHarmMemories = true;
    public bool adoptiveParentWelcomeMemory = true;
    public bool fosterParentWelcomeMemory = true;
    public bool fosterChildBondMemory = true;

    public RjwDrugMode rjwDrugCompatibilityMode = RjwDrugMode.Auto;
    public IntimacyPostpartumMode intimacyPostpartumMode = IntimacyPostpartumMode.FertilityExpanded;
    public EggPostpartumMode genderWorksEggPostpartumMode = EggPostpartumMode.FertilityExpanded;
    public bool sizedApparelLactationGrowth = true;
    public bool rjwBreastLactationScaling;
    public List<float> rjwBreastLactationFactors = BreastLactationScaling.DefaultFactorList();

    public override void ExposeData()
    {
        base.ExposeData();

        if (Scribe.mode == LoadSaveMode.Saving)
        {
            settingsVersion = CurrentSettingsVersion;
        }

        Scribe_Values.Look(ref settingsVersion, "settingsVersion", 0, forceSave: true);

        Scribe_Values.Look(ref fertilityCycles, "fertilityCycles", true);
        Scribe_Values.Look(ref menopause, "menopause", true);
        Scribe_Values.Look(ref multibirthRebalance, "multibirthRebalance", true);
        Scribe_Values.Look(ref multibirthGeneCommonality, "multibirthGeneCommonality", DefaultMultibirthGeneCommonality);

        Scribe_Values.Look(ref postpartumRecovery, "postpartumRecovery", true);
        Scribe_Values.Look(ref postpartumExhaustionOverride, "postpartumExhaustionOverride", true);
        Scribe_Values.Look(ref postpartumSadness, "postpartumSadness", true);

        Scribe_Values.Look(ref procedureOverrides, "procedureOverrides", true);
        Scribe_Values.Look(ref lactationSystem, "lactationSystem", true);
        Scribe_Values.Look(ref advancedLactationSettings, "advancedLactationSettings");
        Scribe_Values.Look(ref maleLactationFromGene, "maleLactationFromGene");
        Scribe_Values.Look(ref maleLactationFromRole, "maleLactationFromRole", true);
        Scribe_Values.Look(ref maleLactationFromImplant, "maleLactationFromImplant", true);
        Scribe_Collections.Look(ref xenotypeLactationSettings, "xenotypeLactationSettings", LookMode.Deep);

        Scribe_Values.Look(ref disableAdoptionQuests, "disableAdoptionQuests");
        Scribe_Values.Look(ref disableFosterQuests, "disableFosterQuests");

        Scribe_Values.Look(ref babyBondingMemories, "babyBondingMemories", true);
        Scribe_Values.Look(ref supportedPostpartumCareMemory, "supportedPostpartumCareMemory", true);
        Scribe_Values.Look(ref neglectedPostpartumCareMemory, "neglectedPostpartumCareMemory", true);
        Scribe_Values.Look(ref singleParentStrain, "singleParentStrain", true);
        Scribe_Values.Look(ref coparentSupport, "coparentSupport", true);
        Scribe_Values.Look(ref childHarmMemories, "childHarmMemories", true);
        Scribe_Values.Look(ref adoptiveParentWelcomeMemory, "adoptiveParentWelcomeMemory", true);
        Scribe_Values.Look(ref fosterParentWelcomeMemory, "fosterParentWelcomeMemory", true);
        Scribe_Values.Look(ref fosterChildBondMemory, "fosterChildBondMemory", true);

        Scribe_Values.Look(ref rjwDrugCompatibilityMode, "rjwDrugCompatibilityMode", RjwDrugMode.Auto);
        Scribe_Values.Look(ref intimacyPostpartumMode, "intimacyPostpartumMode", IntimacyPostpartumMode.FertilityExpanded);
        Scribe_Values.Look(ref genderWorksEggPostpartumMode, "genderWorksEggPostpartumMode", EggPostpartumMode.FertilityExpanded);
        Scribe_Values.Look(ref sizedApparelLactationGrowth, "sizedApparelLactationGrowth", true);
        Scribe_Values.Look(ref rjwBreastLactationScaling, "rjwBreastLactationScaling");
        Scribe_Collections.Look(ref rjwBreastLactationFactors, "rjwBreastLactationFactors", LookMode.Value);

        if (Scribe.mode != LoadSaveMode.LoadingVars)
        {
            return;
        }

        if (settingsVersion < 1)
        {
            MigrateLegacySettings();
        }

        ValidateCompatibilitySettings();
        ValidateXenotypeLactationSettings();
        multibirthGeneCommonality = Mathf.Clamp01(multibirthGeneCommonality);
        settingsVersion = CurrentSettingsVersion;
    }

    public void ResetFeatureSettings()
    {
        fertilityCycles = true;
        menopause = true;
        multibirthRebalance = true;
        multibirthGeneCommonality = DefaultMultibirthGeneCommonality;

        postpartumRecovery = true;
        postpartumExhaustionOverride = true;
        postpartumSadness = true;

        procedureOverrides = true;
        lactationSystem = true;
        advancedLactationSettings = false;
        maleLactationFromGene = false;
        maleLactationFromRole = true;
        maleLactationFromImplant = true;
        xenotypeLactationSettings = new List<XenotypeSettings>();
        xenotypeLactationSettingsByDefName = null;

        disableAdoptionQuests = false;
        disableFosterQuests = false;

        babyBondingMemories = true;
        supportedPostpartumCareMemory = true;
        neglectedPostpartumCareMemory = true;
        singleParentStrain = true;
        coparentSupport = true;
        childHarmMemories = true;
        adoptiveParentWelcomeMemory = true;
        fosterParentWelcomeMemory = true;
        fosterChildBondMemory = true;
    }

    private void MigrateLegacySettings()
    {
        bool legacyMultipleBirths = true;
        bool legacyPostpartumExhaustionRebalance = true;
        bool legacyAgeDependentVasectomyReversal = true;

        bool legacyLactationImplant = true;
        bool legacySelfMilking = true;

        bool legacyEmergencyAdoptionRequests = true;
        bool legacyFosterPlacementRequests = true;

        bool legacyPostpartumCareMemories = true;

        Scribe_Values.Look(ref legacyMultipleBirths, "multipleBirths", true);
        Scribe_Values.Look(ref legacyPostpartumExhaustionRebalance, "postpartumExhaustionRebalance", true);
        Scribe_Values.Look(ref legacyAgeDependentVasectomyReversal, "ageDependentVasectomyReversal", true);

        Scribe_Values.Look(ref legacyLactationImplant, "lactationImplant", true);
        Scribe_Values.Look(ref legacySelfMilking, "selfMilking", true);

        Scribe_Values.Look(ref legacyEmergencyAdoptionRequests, "emergencyAdoptionRequests", true);
        Scribe_Values.Look(ref legacyFosterPlacementRequests, "fosterPlacementRequests", true);

        Scribe_Values.Look(ref legacyPostpartumCareMemories, "postpartumCareMemories", true);

        multibirthRebalance = legacyMultipleBirths;
        postpartumExhaustionOverride = legacyPostpartumExhaustionRebalance;
        procedureOverrides = legacyAgeDependentVasectomyReversal;

        lactationSystem = legacyLactationImplant
            || legacySelfMilking;

        disableAdoptionQuests = !legacyEmergencyAdoptionRequests;
        disableFosterQuests = !legacyFosterPlacementRequests;

        supportedPostpartumCareMemory = legacyPostpartumCareMemories;
        neglectedPostpartumCareMemory = legacyPostpartumCareMemories;
    }

    private void ValidateCompatibilitySettings()
    {
        if (rjwBreastLactationFactors == null
            || rjwBreastLactationFactors.Count != BreastLactationScaling.TierCount)
        {
            rjwBreastLactationFactors = BreastLactationScaling.DefaultFactorList();
        }

        for (int i = 0; i < rjwBreastLactationFactors.Count; i++)
        {
            float factor = rjwBreastLactationFactors[i];
            rjwBreastLactationFactors[i] = float.IsNaN(factor) || float.IsInfinity(factor)
                ? BreastLactationScaling.DefaultFactorList()[i]
                : Mathf.Clamp(factor, BreastLactationScaling.MinimumFactor, BreastLactationScaling.MaximumFactor);
        }

        if (rjwDrugCompatibilityMode != RjwDrugMode.Auto
            && rjwDrugCompatibilityMode != RjwDrugMode.FertilityExpanded
            && rjwDrugCompatibilityMode != RjwDrugMode.RimJobWorld)
        {
            rjwDrugCompatibilityMode = RjwDrugMode.Auto;
        }

        if (intimacyPostpartumMode != IntimacyPostpartumMode.FertilityExpanded
            && intimacyPostpartumMode != IntimacyPostpartumMode.Intimacy)
        {
            intimacyPostpartumMode = IntimacyPostpartumMode.FertilityExpanded;
        }

        if (genderWorksEggPostpartumMode != EggPostpartumMode.FertilityExpanded
            && genderWorksEggPostpartumMode != EggPostpartumMode.GenderWorks)
        {
            genderWorksEggPostpartumMode = EggPostpartumMode.FertilityExpanded;
        }
    }

    public XenotypeSettings GetOrCreateXenotypeLactationSetting(string xenotypeDefName)
    {
        if (xenotypeDefName.NullOrEmpty())
        {
            return null;
        }

        EnsureXenotypeLactationSettingsIndex();
        if (xenotypeLactationSettingsByDefName.TryGetValue(
            xenotypeDefName,
            out XenotypeSettings setting))
        {
            return setting;
        }

        setting = new XenotypeSettings(xenotypeDefName);
        xenotypeLactationSettings.Add(setting);
        xenotypeLactationSettingsByDefName.Add(xenotypeDefName, setting);
        return setting;
    }

    internal bool TryGetXenotypeLactationSetting(
        string xenotypeDefName,
        out XenotypeSettings setting)
    {
        setting = null;
        if (xenotypeDefName.NullOrEmpty())
        {
            return false;
        }

        EnsureXenotypeLactationSettingsIndex();
        return xenotypeLactationSettingsByDefName.TryGetValue(xenotypeDefName, out setting);
    }

    private void ValidateXenotypeLactationSettings()
    {
        if (xenotypeLactationSettings == null)
        {
            xenotypeLactationSettings = new List<XenotypeSettings>();
        }

        HashSet<string> seenDefNames = new HashSet<string>();
        for (int i = xenotypeLactationSettings.Count - 1; i >= 0; i--)
        {
            XenotypeSettings setting = xenotypeLactationSettings[i];
            if (setting == null
                || setting.xenotypeDefName.NullOrEmpty()
                || !seenDefNames.Add(setting.xenotypeDefName))
            {
                xenotypeLactationSettings.RemoveAt(i);
                continue;
            }

            setting.Validate();
        }

        xenotypeLactationSettingsByDefName = null;
    }

    private void EnsureXenotypeLactationSettingsIndex()
    {
        if (xenotypeLactationSettingsByDefName != null)
        {
            return;
        }

        xenotypeLactationSettingsByDefName = new Dictionary<string, XenotypeSettings>();
        for (int i = 0; i < xenotypeLactationSettings.Count; i++)
        {
            XenotypeSettings setting = xenotypeLactationSettings[i];
            if (setting != null && !setting.xenotypeDefName.NullOrEmpty())
            {
                xenotypeLactationSettingsByDefName[setting.xenotypeDefName] = setting;
            }
        }
    }
}
