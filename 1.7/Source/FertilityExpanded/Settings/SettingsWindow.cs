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
    public override string SettingsCategory()
    {
        return "FE_ModName".Translate();
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        base.DoSettingsWindowContents(inRect);

        Rect contentRect = new Rect(
            inRect.x,
            inRect.y + TabDrawer.TabHeight + 8f,
            inRect.width,
            inRect.height - TabDrawer.TabHeight - 8f);
        List<TabRecord> tabs = new List<TabRecord>
        {
            new TabRecord(
                "FE_SettingsTab".Translate(),
                () => selectedSettingsTab = SettingsTab.FertilityExpanded,
                () => selectedSettingsTab == SettingsTab.FertilityExpanded),
            new TabRecord(
                "FE_CompatibilityTab".Translate(),
                () => selectedSettingsTab = SettingsTab.Compatibility,
                () => selectedSettingsTab == SettingsTab.Compatibility)
        };

        Widgets.DrawMenuSection(contentRect);
        TabDrawer.DrawTabs(contentRect, tabs);

        Rect settingsRect = contentRect.ContractedBy(10f);
        if (selectedSettingsTab == SettingsTab.Compatibility)
        {
            DrawCompatibilitySettings(settingsRect);
        }
        else
        {
            DrawFertilityExpandedSettings(settingsRect);
        }
    }

    private void DrawFertilityExpandedSettings(Rect inRect)
    {
        FertilityExpandedSettings settings = Settings;
        bool showAdvancedLactationControls = settings.advancedLactationSettings;
        int lactationRows = showAdvancedLactationControls ? 8 : 2;
        int[] groupRows = { 5, 3, 1, lactationRows, 2, 9 };
        float viewHeight = 48f;
        for (int i = 0; i < groupRows.Length; i++)
        {
            viewHeight += FeatureGroupHeight(groupRows[i]) + FeatureGroupGap;
        }

        float viewWidth = inRect.width - ScrollbarWidth;
        Rect viewRect = new Rect(0f, 0f, viewWidth, viewHeight);
        Widgets.BeginScrollView(inRect, ref fertilitySettingsScrollPosition, viewRect);

        Rect introductionRect = new Rect(0f, 3f, viewWidth - 190f, 38f);
        Widgets.Label(introductionRect, "FE_SettingsIntroduction".Translate());
        Rect resetRect = new Rect(viewWidth - 175f, 5f, 175f, 30f);
        if (Widgets.ButtonText(resetRect, "FE_ResetDefaults".Translate()))
        {
            settings.ResetFeatureSettings();
        }

        float y = 48f;
        DrawFeatureGroup(viewWidth, ref y, "FE_SettingsReproduction".Translate(), 5, listing =>
        {
            FeatureCheckbox(
                listing,
                "FE_SettingsFertilityCycle".Translate(),
                ref settings.fertilityCycles,
                "FE_SettingsFertilityCycleTip".Translate());
            FeatureCheckbox(
                listing,
                "FE_SettingsMenopause".Translate(),
                ref settings.menopause,
                "FE_SettingsMenopauseTip".Translate(),
                settings.fertilityCycles,
                24f);
            FeatureCheckbox(
                listing,
                "FE_SettingsMultibirth".Translate(),
                ref settings.multibirthRebalance,
                "FE_SettingsMultibirthTip".Translate());
            FeatureSlider(
                listing,
                "FE_SettingsGeneCommonality".Translate(),
                ref settings.multibirthGeneCommonality,
                0f,
                1f,
                0.01f,
                "FE_SettingsGeneCommonalityTip".Translate());
        });

        DrawFeatureGroup(viewWidth, ref y, "FE_SettingsPostpartum".Translate(), 3, listing =>
        {
            FeatureCheckbox(
                listing,
                "FE_SettingsRecovery".Translate(),
                ref settings.postpartumRecovery,
                "FE_SettingsRecoveryTip".Translate());
            FeatureCheckbox(
                listing,
                "FE_SettingsExhaustion".Translate(),
                ref settings.postpartumExhaustionOverride,
                "FE_SettingsExhaustionTip".Translate());
            FeatureCheckbox(
                listing,
                "FE_SettingsSadness".Translate(),
                ref settings.postpartumSadness,
                "FE_SettingsSadnessTip".Translate());
        });

        DrawFeatureGroup(viewWidth, ref y, "FE_SettingsProcedures".Translate(), 1, listing =>
        {
            FeatureCheckbox(
                listing,
                "FE_SettingsProcedureOverrides".Translate(),
                ref settings.procedureOverrides,
                "FE_SettingsProcedureOverridesTip".Translate());
        });

        DrawFeatureGroup(viewWidth, ref y, "FE_SettingsLactation".Translate(), lactationRows, listing =>
        {
            FeatureCheckbox(
                listing,
                "FE_SettingsLactationSystem".Translate(),
                ref settings.lactationSystem,
                "FE_SettingsLactationSystemTip".Translate());
            FeatureCheckbox(
                listing,
                "FE_SettingsAdvancedLactation".Translate(),
                ref settings.advancedLactationSettings,
                "FE_SettingsAdvancedLactationTip".Translate(),
                settings.lactationSystem && ModsConfig.BiotechActive,
                24f);

            if (showAdvancedLactationControls)
            {
                bool controlsEnabled = settings.lactationSystem
                    && settings.advancedLactationSettings
                    && ModsConfig.BiotechActive;
                XenotypeDef xenotype = SelectedLactationXenotype();
                XenotypeSettings xenotypeSettings = settings.GetOrCreateXenotypeLactationSetting(xenotype?.defName);
                FeatureDropdown(
                    listing,
                    "FE_SettingsXenotype".Translate(),
                    XenotypeProfiles.XenotypeLabel(xenotype),
                    OpenLactationXenotypeMenu,
                    "FE_SettingsXenotypeTip".Translate(),
                    controlsEnabled,
                    24f);

                if (xenotypeSettings != null)
                {
                    FeatureMultiplierSlider(
                        listing,
                        "FE_SettingsStorageCapacity".Translate(),
                        ref xenotypeSettings.storageCapacityMultiplier,
                        "FE_SettingsStorageCapacityTip".Translate(),
                        controlsEnabled,
                        24f);
                    FeatureMultiplierSlider(
                        listing,
                        "FE_SettingsProductionRate".Translate(),
                        ref xenotypeSettings.productionRateMultiplier,
                        "FE_SettingsProductionRateTip".Translate(),
                        controlsEnabled,
                        24f);
                    FeatureDropdown(
                        listing,
                        "FE_SettingsProducedMilk".Translate(),
                        XenotypeProfiles.SelectedMilkLabel(xenotypeSettings),
                        () => OpenProducedMilkMenu(xenotypeSettings),
                        "FE_SettingsProducedMilkTip".Translate(),
                        controlsEnabled,
                        24f);
                }
            }
        });

        DrawFeatureGroup(viewWidth, ref y, "FE_SettingsFamily".Translate(), 2, listing =>
        {
            FeatureCheckbox(
                listing,
                "FE_SettingsDisableAdoption".Translate(),
                ref settings.disableAdoptionQuests,
                "FE_SettingsDisableAdoptionTip".Translate());
            FeatureCheckbox(
                listing,
                "FE_SettingsDisableFoster".Translate(),
                ref settings.disableFosterQuests,
                "FE_SettingsDisableFosterTip".Translate());
        });

        DrawFeatureGroup(viewWidth, ref y, "FE_SettingsThoughts".Translate(), 9, listing =>
        {
            FeatureCheckbox(
                listing,
                "FE_SettingsBonding".Translate(),
                ref settings.babyBondingMemories,
                "FE_SettingsBondingTip".Translate());
            FeatureCheckbox(
                listing,
                "FE_SettingsSupported".Translate(),
                ref settings.supportedPostpartumCareMemory,
                "FE_SettingsSupportedTip".Translate());
            FeatureCheckbox(
                listing,
                "FE_SettingsNeglected".Translate(),
                ref settings.neglectedPostpartumCareMemory,
                "FE_SettingsNeglectedTip".Translate());
            FeatureCheckbox(
                listing,
                "FE_SettingsSingleParent".Translate(),
                ref settings.singleParentStrain,
                "FE_SettingsSingleParentTip".Translate());
            FeatureCheckbox(
                listing,
                "FE_SettingsCoparent".Translate(),
                ref settings.coparentSupport,
                "FE_SettingsCoparentTip".Translate());
            FeatureCheckbox(
                listing,
                "FE_SettingsChildHarm".Translate(),
                ref settings.childHarmMemories,
                "FE_SettingsChildHarmTip".Translate());
            FeatureCheckbox(
                listing,
                "FE_SettingsAdoptiveWelcome".Translate(),
                ref settings.adoptiveParentWelcomeMemory,
                "FE_SettingsAdoptiveWelcomeTip".Translate());
            FeatureCheckbox(
                listing,
                "FE_SettingsFosterWelcome".Translate(),
                ref settings.fosterParentWelcomeMemory,
                "FE_SettingsFosterWelcomeTip".Translate());
            FeatureCheckbox(
                listing,
                "FE_SettingsFosterBond".Translate(),
                ref settings.fosterChildBondMemory,
                "FE_SettingsFosterBondTip".Translate());
        });

        Widgets.EndScrollView();
    }

    private void DrawCompatibilitySettings(Rect inRect)
    {
        bool rjwActive = ModDetection.RimJobWorldActive;
        bool intimacyActive = ModDetection.IntimacyActive;
        bool genderWorksActive = ModDetection.GenderWorksActive;
        bool sizedApparelActive = ModDetection.SizedApparelActive;
        bool anyCompatibilityActive = rjwActive || intimacyActive || genderWorksActive || sizedApparelActive;

        float viewHeight = 0f;
        if (rjwActive)
        {
            viewHeight += FeatureGroupHeight(5) + FeatureGroupGap;
        }

        if (intimacyActive)
        {
            viewHeight += FeatureGroupHeight(4) + FeatureGroupGap;
        }

        if (genderWorksActive)
        {
            viewHeight += FeatureGroupHeight(4) + FeatureGroupGap;
        }

        if (sizedApparelActive)
        {
            viewHeight += FeatureGroupHeight(1) + FeatureGroupGap;
        }

        if (!anyCompatibilityActive)
        {
            viewHeight = FeatureGroupHeight(2) + FeatureGroupGap;
        }

        viewHeight = Math.Max(viewHeight, inRect.height);
        float viewWidth = inRect.width - ScrollbarWidth;
        Rect viewRect = new Rect(0f, 0f, viewWidth, viewHeight);
        Widgets.BeginScrollView(inRect, ref compatibilitySettingsScrollPosition, viewRect);

        float y = 0f;
        if (rjwActive)
        {
            DrawFeatureGroup(viewWidth, ref y, "FE_SettingsRjwOwnership".Translate(), 5, listing =>
            {
                RjwDrugCompatibilityOption(
                    listing,
                    RjwDrugMode.Auto,
                    "FE_SettingsRjwAuto".Translate(),
                    "FE_SettingsRjwAutoTip".Translate());
                RjwDrugCompatibilityOption(
                    listing,
                    RjwDrugMode.FertilityExpanded,
                    "FE_SettingsRjwPreferFe".Translate(),
                    "FE_SettingsRjwPreferFeTip".Translate());
                RjwDrugCompatibilityOption(
                    listing,
                    RjwDrugMode.RimJobWorld,
                    "FE_SettingsRjwPreferRjw".Translate(),
                    "FE_SettingsRjwPreferRjwTip".Translate());
                SettingsText(
                    listing,
                    "FE_SettingsCurrent".Translate(RjwDrugs.CurrentStatusLabel.Named("STATUS")),
                    2);
            });
        }

        if (intimacyActive)
        {
            DrawFeatureGroup(viewWidth, ref y, "FE_SettingsIntimacyRecovery".Translate(), 4, listing =>
            {
                IntimacyPostpartumOption(
                    listing,
                    IntimacyPostpartumMode.FertilityExpanded,
                    "FE_SettingsFeRecovery".Translate());
                IntimacyPostpartumOption(
                    listing,
                    IntimacyPostpartumMode.Intimacy,
                    "FE_SettingsIntimacyRecoveryValue".Translate());
                SettingsText(listing, "FE_SettingsBirthRecoveryFuture".Translate(), 2);
            });
        }

        if (genderWorksActive)
        {
            DrawFeatureGroup(viewWidth, ref y, "FE_SettingsGenderWorksRecovery".Translate(), 4, listing =>
            {
                GenderWorksEggPostpartumOption(
                    listing,
                    EggPostpartumMode.FertilityExpanded,
                    "FE_SettingsFeRecovery".Translate());
                GenderWorksEggPostpartumOption(
                    listing,
                    EggPostpartumMode.GenderWorks,
                    "FE_SettingsGenderWorksRecoveryValue".Translate());
                SettingsText(listing, "FE_SettingsEggRecoveryFuture".Translate(), 2);
            });
        }

        if (sizedApparelActive)
        {
            DrawFeatureGroup(viewWidth, ref y, "FE_SettingsSizedApparel".Translate(), 1, listing =>
            {
                bool previousValue = Settings.sizedApparelLactationGrowth;
                FeatureCheckbox(
                    listing,
                    "FE_SettingsLactationGrowth".Translate(),
                    ref Settings.sizedApparelLactationGrowth,
                    "FE_SettingsLactationGrowthTip".Translate(),
                    ModsConfig.BiotechActive);
                if (previousValue != Settings.sizedApparelLactationGrowth)
                {
                    SizedApparelCompat.RefreshAllPawns();
                }
            });
        }

        if (!anyCompatibilityActive)
        {
            DrawFeatureGroup(viewWidth, ref y, "FE_SettingsCompatibility".Translate(), 2, listing =>
            {
                SettingsText(
                    listing,
                    "FE_SettingsNoCompatibility".Translate(),
                    2);
            });
        }

        Widgets.EndScrollView();
    }

    private static float FeatureGroupHeight(int rowCount)
    {
        return FeatureGroupPadding * 2f + FeatureGroupHeaderHeight + rowCount * FeatureRowHeight;
    }

    private static void DrawFeatureGroup(
        float width,
        ref float y,
        string title,
        int rowCount,
        Action<Listing_Standard> drawContents)
    {
        float height = FeatureGroupHeight(rowCount);
        Rect sectionRect = new Rect(0f, y, width, height);
        Widgets.DrawMenuSection(sectionRect);

        GameFont previousFont = Text.Font;
        Text.Font = GameFont.Medium;
        Widgets.Label(
            new Rect(
                sectionRect.x + FeatureGroupPadding,
                sectionRect.y + FeatureGroupPadding - 2f,
                sectionRect.width - FeatureGroupPadding * 2f,
                FeatureGroupHeaderHeight),
            title);
        Text.Font = previousFont;

        Rect listingRect = new Rect(
            sectionRect.x + FeatureGroupPadding,
            sectionRect.y + FeatureGroupPadding + FeatureGroupHeaderHeight,
            sectionRect.width - FeatureGroupPadding * 2f,
            rowCount * FeatureRowHeight);
        Listing_Standard listing = new Listing_Standard();
        listing.Begin(listingRect);
        drawContents(listing);
        listing.End();

        y += height + FeatureGroupGap;
    }

    private static void FeatureCheckbox(
        Listing_Standard listing,
        string label,
        ref bool value,
        string tooltip,
        bool enabled = true,
        float indent = 0f)
    {
        Rect row = listing.GetRect(FeatureRowHeight - 4f);
        row.xMin += indent;

        Color previousColor = GUI.color;
        if (!enabled)
        {
            GUI.color = Color.gray;
        }

        Widgets.CheckboxLabeled(row, label, ref value, disabled: !enabled);
        GUI.color = previousColor;

        if (!tooltip.NullOrEmpty())
        {
            TooltipHandler.TipRegion(row, tooltip);
        }

        listing.Gap(4f);
    }

    private static void SettingsText(Listing_Standard listing, string text, int rowCount)
    {
        Rect row = listing.GetRect(rowCount * FeatureRowHeight - 4f);
        Widgets.Label(row, text);
        listing.Gap(4f);
    }

    private static void FeatureSlider(
        Listing_Standard listing,
        string label,
        ref float value,
        float minimum,
        float maximum,
        float roundTo,
        string tooltip)
    {
        Rect row = listing.GetRect(FeatureRowHeight * 2f - 4f);
        Rect sliderRect = new Rect(
            row.x,
            row.y + (row.height - SliderControlHeight) / 2f,
            row.width,
            SliderControlHeight);
        value = Widgets.HorizontalSlider(
            sliderRect,
            value,
            minimum,
            maximum,
            middleAlignment: false,
            "FE_LabelValue".Translate(label.Named("LABEL"), value.ToStringPercent().Named("VALUE")),
            minimum.ToStringPercent(),
            maximum.ToStringPercent(),
            roundTo);

        if (!tooltip.NullOrEmpty())
        {
            TooltipHandler.TipRegion(row, tooltip);
        }

        listing.Gap(4f);
    }

    private static void FeatureMultiplierSlider(
        Listing_Standard listing,
        string label,
        ref float value,
        string tooltip,
        bool enabled,
        float indent)
    {
        Rect row = listing.GetRect(FeatureRowHeight * 2f - 4f);
        row.xMin += indent;
        Rect sliderRect = new Rect(
            row.x,
            row.y + (row.height - SliderControlHeight) / 2f,
            row.width,
            SliderControlHeight);

        Color previousColor = GUI.color;
        bool previousEnabled = GUI.enabled;
        if (!enabled)
        {
            GUI.color = Color.gray;
            GUI.enabled = false;
        }

        float adjustedValue = Widgets.HorizontalSlider(
            sliderRect,
            value,
            XenotypeProfiles.MinimumMultiplier,
            XenotypeProfiles.MaximumMultiplier,
            middleAlignment: false,
            "FE_LabelMultiplier".Translate(label.Named("LABEL"), value.ToStringPercent().Named("VALUE")),
            "FE_Multiplier".Translate(XenotypeProfiles.MinimumMultiplier.ToStringPercent().Named("VALUE")),
            "FE_Multiplier".Translate(XenotypeProfiles.MaximumMultiplier.ToStringPercent().Named("VALUE")),
            XenotypeProfiles.MultiplierStep);
        if (enabled)
        {
            value = adjustedValue;
        }

        GUI.enabled = previousEnabled;
        GUI.color = previousColor;
        TooltipHandler.TipRegion(row, tooltip);
        listing.Gap(4f);
    }

    private static void FeatureDropdown(
        Listing_Standard listing,
        string label,
        string selectedLabel,
        Action openMenu,
        string tooltip,
        bool enabled,
        float indent)
    {
        Rect row = listing.GetRect(FeatureRowHeight - 4f);
        row.xMin += indent;
        Rect labelRect = new Rect(row.x, row.y, row.width * 0.34f, row.height);
        Rect buttonRect = new Rect(labelRect.xMax + 8f, row.y, row.xMax - labelRect.xMax - 8f, row.height);

        Color previousColor = GUI.color;
        if (!enabled)
        {
            GUI.color = Color.gray;
        }

        Widgets.Label(labelRect, label);
        if (Widgets.ButtonText(buttonRect, selectedLabel, active: enabled))
        {
            openMenu();
        }

        GUI.color = previousColor;
        TooltipHandler.TipRegion(row, tooltip);
        listing.Gap(4f);
    }

    private XenotypeDef SelectedLactationXenotype()
    {
        XenotypeDef xenotype = XenotypeProfiles.GetXenotype(selectedLactationXenotypeDefName);
        selectedLactationXenotypeDefName = xenotype?.defName;
        return xenotype;
    }

    private void OpenLactationXenotypeMenu()
    {
        IReadOnlyList<XenotypeDef> xenotypes = XenotypeProfiles.Xenotypes;
        List<FloatMenuOption> options = new List<FloatMenuOption>(xenotypes.Count);
        for (int i = 0; i < xenotypes.Count; i++)
        {
            XenotypeDef xenotype = xenotypes[i];
            XenotypeDef selectedXenotype = xenotype;
            options.Add(new FloatMenuOption(
                XenotypeProfiles.XenotypeLabel(xenotype),
                () => selectedLactationXenotypeDefName = selectedXenotype.defName));
        }

        if (options.Count > 0)
        {
            Find.WindowStack.Add(new FloatMenu(options));
        }
    }

    private static void OpenProducedMilkMenu(XenotypeSettings setting)
    {
        IReadOnlyList<ThingDef> milkTypes = XenotypeProfiles.MilkTypes;
        List<FloatMenuOption> options = new List<FloatMenuOption>(milkTypes.Count);
        ThingDef dominantMilk = XenotypeProfiles.DominantMilkDef;
        if (dominantMilk != null)
        {
            options.Add(new FloatMenuOption(
                "FE_DefaultMilk".Translate(XenotypeProfiles.MilkLabel(dominantMilk).Named("MILK")),
                () => setting.producedMilkDefName = null,
                dominantMilk));
        }

        for (int i = 0; i < milkTypes.Count; i++)
        {
            ThingDef milkDef = milkTypes[i];
            if (milkDef == null || milkDef == dominantMilk)
            {
                continue;
            }

            ThingDef selectedMilk = milkDef;
            options.Add(new FloatMenuOption(
                XenotypeProfiles.MilkLabel(milkDef),
                () => setting.producedMilkDefName = selectedMilk.defName,
                milkDef));
        }

        if (options.Count > 0)
        {
            Find.WindowStack.Add(new FloatMenu(options));
        }
    }

    private static bool SettingsRadioButton(
        Listing_Standard listing,
        string label,
        bool active,
        string tooltip = null,
        bool enabled = true)
    {
        Rect row = listing.GetRect(FeatureRowHeight - 4f);
        if (!tooltip.NullOrEmpty())
        {
            if (enabled && Mouse.IsOver(row))
            {
                Widgets.DrawHighlight(row);
            }

            TooltipHandler.TipRegion(row, tooltip);
        }

        bool clicked = Widgets.RadioButtonLabeled(row, label, active, disabled: !enabled);
        listing.Gap(4f);
        return enabled && clicked;
    }

    private static void RjwDrugCompatibilityOption(
        Listing_Standard listing,
        RjwDrugMode mode,
        string label,
        string tooltip)
    {
        string restartTooltip = "FE_RestartTooltip".Translate(tooltip.Named("DESCRIPTION"));
        if (SettingsRadioButton(listing, label, Settings.rjwDrugCompatibilityMode == mode, restartTooltip))
        {
            Settings.rjwDrugCompatibilityMode = mode;
        }
    }

    private static void IntimacyPostpartumOption(
        Listing_Standard listing,
        IntimacyPostpartumMode mode,
        string label)
    {
        bool enabled = mode != IntimacyPostpartumMode.FertilityExpanded || Features.PostpartumRecovery;
        string tooltip = enabled ? null : "FE_SettingsRecoveryDisabledTip".Translate();
        if (SettingsRadioButton(listing, label, Settings.intimacyPostpartumMode == mode, tooltip, enabled))
        {
            Settings.intimacyPostpartumMode = mode;
        }
    }

    private static void GenderWorksEggPostpartumOption(
        Listing_Standard listing,
        EggPostpartumMode mode,
        string label)
    {
        bool enabled = mode != EggPostpartumMode.FertilityExpanded || Features.PostpartumRecovery;
        string tooltip = enabled ? null : "FE_SettingsRecoveryDisabledTip".Translate();
        if (SettingsRadioButton(listing, label, Settings.genderWorksEggPostpartumMode == mode, tooltip, enabled))
        {
            Settings.genderWorksEggPostpartumMode = mode;
        }
    }

    private enum SettingsTab
    {
        FertilityExpanded,
        Compatibility
    }
}
