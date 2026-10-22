using System;
using System.Collections.Generic;
using FertilityExpanded.Patches.Compatibility;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace FertilityExpanded;

public partial class FertilityExpandedMod : Mod
{
    public const string PackageId = "lucius.fertility.expanded";

    private const float FeatureGroupGap = 10f;

    private const float FeatureGroupHeaderHeight = 30f;

    private const float FeatureGroupPadding = 10f;

    private const float FeatureRowHeight = 28f;

    private const float SliderControlHeight = 24f;

    private const float ScrollbarWidth = 18f;

    private static bool patched;

    private SettingsTab selectedSettingsTab;

    private Vector2 fertilitySettingsScrollPosition;

    private Vector2 compatibilitySettingsScrollPosition;

    private string selectedLactationXenotypeDefName;

    public static FertilityExpandedSettings Settings { get; private set; }

    public FertilityExpandedMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<FertilityExpandedSettings>();
        LongEventHandler.ExecuteWhenFinished(PatchAfterDefsLoaded);
    }

    public override void WriteSettings()
    {
        base.WriteSettings();
        FeatureSettings.Apply();
    }

    private static void PatchAfterDefsLoaded()
    {
        if (patched)
        {
            return;
        }

        patched = true;
        IntimacyRecovery.Initialize();
        ExternalMilkCompat.Initialize();
        XenotypeProfiles.Initialize();
        AdoptionRelationsCompat.Initialize();
        Harmony harmony = new Harmony(PackageId);
        harmony.PatchAll();
        PatchRegistration.Apply(harmony);
        RjwDrugs.InitializeSession();
        FeatureSettings.Apply();
    }
}
