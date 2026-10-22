using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace FertilityExpanded.Patches.Compatibility;

internal static class ModDetection
{
    public const string IntimacyPackageId = "LovelyDovey.Sex.WithEuterpe";
    public const string GenderWorksPackageId = "LovelyDovey.Sex.WithRosaline";
    public const string WayBetterRomancePackageId = "divineDerivative.Romance";
    public const string LactationExpansionPackageId = "Euterpe.MilkyTitfuck";
    public const string EqualMilkingPackageId = "akaster.equalmilking";
    public const string HumanCattlePackageId = "Doop.HumanCattle";
    public const string RoosMinotaurPackageId = "tug.Minotaur";
    public const string RimJobWorldPackageId = "rim.job.world";
    public const string LuciusRimJobWorldPackageId = "lucius.rjw";
    public const string MenstruationPackageId = "rjw.menstruation";
    public const string RizrynFamilyTreePackageId = "rizryn.familytree";
    public const string LuciusFamilyTreePackageId = "lucius.familytree";
    public const string FluffyFamilyTreePackageId = "Garethp.FamilyTree";
    public const string FamilyRelationsAdoptionPackageId = "Rycon.FamilyRelationsAdoption";
    public const string FamilyTiesPackageId = "dickus.familyties";
    public const string SizedApparelPackageId = "OTYOTY.SizedApparel";
    public const string FamilyIsEverythingPackageId = "crycore.familyiseverything";
    public const string RomanceOnTheRimPackageId = "telardo.RomanceOnTheRim";

    private static readonly string[] LuciusRimJobWorldFolderNames =
    {
        "Lucius RJW",
        "[RJW]Lucius RJW"
    };

    private static readonly PropertyInfo rimJobWorldUsesVanillaPregnancyProperty;

    static ModDetection()
    {
        HashSet<string> packageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        HashSet<string> folderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        List<ModContentPack> mods = LoadedModManager.RunningModsListForReading;
        for (int i = 0; i < mods.Count; i++)
        {
            ModContentPack mod = mods[i];
            if (mod == null)
            {
                continue;
            }

            if (!mod.PackageId.NullOrEmpty())
            {
                packageIds.Add(mod.PackageId);
            }

            if (!mod.PackageIdPlayerFacing.NullOrEmpty())
            {
                packageIds.Add(mod.PackageIdPlayerFacing);
            }

            if (!mod.FolderName.NullOrEmpty())
            {
                folderNames.Add(mod.FolderName);
            }
        }

        IntimacyActive = packageIds.Contains(IntimacyPackageId);
        GenderWorksActive = packageIds.Contains(GenderWorksPackageId);
        WayBetterRomanceActive = packageIds.Contains(WayBetterRomancePackageId);
        LactationExpansionActive = packageIds.Contains(LactationExpansionPackageId);
        EqualMilkingActive = packageIds.Contains(EqualMilkingPackageId);
        HumanCattleActive = packageIds.Contains(HumanCattlePackageId);
        RoosMinotaurActive = packageIds.Contains(RoosMinotaurPackageId);
        RegularRimJobWorldActive = packageIds.Contains(RimJobWorldPackageId);
        LuciusRimJobWorldActive = packageIds.Contains(LuciusRimJobWorldPackageId)
            || ContainsAny(folderNames, LuciusRimJobWorldFolderNames);
        MenstruationActive = packageIds.Contains(MenstruationPackageId);
        RizrynFamilyTreeActive = packageIds.Contains(RizrynFamilyTreePackageId);
        LuciusFamilyTreeActive = packageIds.Contains(LuciusFamilyTreePackageId);
        FluffyFamilyTreeActive = packageIds.Contains(FluffyFamilyTreePackageId);
        FamilyRelationsAdoptionActive = packageIds.Contains(FamilyRelationsAdoptionPackageId);
        FamilyTiesActive = packageIds.Contains(FamilyTiesPackageId);
        SizedApparelActive = packageIds.Contains(SizedApparelPackageId);
        FamilyIsEverythingActive = packageIds.Contains(FamilyIsEverythingPackageId);
        RomanceOnTheRimActive = packageIds.Contains(RomanceOnTheRimPackageId);
        ExternalMilkSystemActive = LactationExpansionActive
            || EqualMilkingActive
            || HumanCattleActive;
        LactationImplantMaintenanceBlocked = HumanCattleActive;
        RimJobWorldActive = RegularRimJobWorldActive
            || LuciusRimJobWorldActive;
        rimJobWorldUsesVanillaPregnancyProperty = FindRimJobWorldUsesVanillaPregnancyProperty();
    }

    public static bool IntimacyActive { get; }

    public static bool GenderWorksActive { get; }

    public static bool WayBetterRomanceActive { get; }

    public static bool LactationExpansionActive { get; }

    public static bool EqualMilkingActive { get; }

    public static bool HumanCattleActive { get; }

    public static bool RoosMinotaurActive { get; }

    public static bool ExternalMilkSystemActive { get; }

    public static bool LactationImplantMaintenanceBlocked { get; }

    public static bool RegularRimJobWorldActive { get; }

    public static bool LuciusRimJobWorldActive { get; }

    public static bool RimJobWorldActive { get; }

    public static bool RimJobWorldUsesVanillaPregnancy
    {
        get
        {
            if (rimJobWorldUsesVanillaPregnancyProperty == null)
            {
                return false;
            }

            try
            {
                return rimJobWorldUsesVanillaPregnancyProperty.GetValue(null, null) is bool value && value;
            }
            catch (Exception ex)
            {
                Log.WarningOnce("[Fertility Expanded] Could not read RJW pregnancy mode: " + ex.Message, 108472303);
                return false;
            }
        }
    }

    public static bool MenstruationActive { get; }

    public static bool RizrynFamilyTreeActive { get; }

    public static bool LuciusFamilyTreeActive { get; }

    public static bool FluffyFamilyTreeActive { get; }

    public static bool FamilyRelationsAdoptionActive { get; }

    public static bool FamilyTiesActive { get; }

    public static bool SizedApparelActive { get; }

    public static bool FamilyIsEverythingActive { get; }

    public static bool RomanceOnTheRimActive { get; }

    private static PropertyInfo FindRimJobWorldUsesVanillaPregnancyProperty()
    {
        if (!RegularRimJobWorldActive)
        {
            return null;
        }

        Type settingsType = AccessTools.TypeByName("rjw.RJWPregnancySettings");
        if (settingsType == null)
        {
            return null;
        }

        return AccessTools.Property(settingsType, "UseVanillaPregnancy");
    }

    private static bool ContainsAny(HashSet<string> values, string[] candidates)
    {
        if (values == null || candidates.NullOrEmpty())
        {
            return false;
        }

        for (int i = 0; i < candidates.Length; i++)
        {
            if (values.Contains(candidates[i]))
            {
                return true;
            }
        }

        return false;
    }

    public static bool TryReadPawnField(object source, string memberName, out Pawn pawn)
    {
        pawn = null;
        if (source == null || memberName.NullOrEmpty())
        {
            return false;
        }

        Type type = source.GetType();
        FieldInfo field = AccessTools.Field(type, memberName);
        if (field != null)
        {
            pawn = field.GetValue(source) as Pawn;
            return pawn != null;
        }

        PropertyInfo property = AccessTools.Property(type, memberName);
        if (property != null)
        {
            pawn = property.GetValue(source, null) as Pawn;
            return pawn != null;
        }

        return false;
    }
}
