namespace FertilityExpanded;

internal static class Features
{
    private static FertilityExpandedSettings Settings => FertilityExpandedMod.Settings;

    public static bool FertilityCycles => Settings?.fertilityCycles ?? true;
    public static bool Menopause => FertilityCycles && (Settings?.menopause ?? true);
    public static bool MultibirthRebalance => Settings?.multibirthRebalance ?? true;
    public static float MultibirthGeneCommonality => Settings?.multibirthGeneCommonality ?? 0.05f;

    public static bool PostpartumRecovery => Settings?.postpartumRecovery ?? true;
    public static bool PostpartumExhaustionOverride => Settings?.postpartumExhaustionOverride ?? true;
    public static bool PostpartumSadness => Settings?.postpartumSadness ?? true;

    public static bool ProcedureOverrides => Settings?.procedureOverrides ?? true;
    public static bool LactationSystem => Settings?.lactationSystem ?? true;
    public static bool AdvancedLactationSettings => LactationSystem
        && (Settings?.advancedLactationSettings ?? false);

    public static bool AdoptionQuestsEnabled => !(Settings?.disableAdoptionQuests ?? false);
    public static bool FosterQuestsEnabled => !(Settings?.disableFosterQuests ?? false);

    public static bool BabyBondingMemories => Settings?.babyBondingMemories ?? true;
    public static bool SupportedPostpartumCareMemory => Settings?.supportedPostpartumCareMemory ?? true;
    public static bool NeglectedPostpartumCareMemory => Settings?.neglectedPostpartumCareMemory ?? true;
    public static bool SingleParentStrain => Settings?.singleParentStrain ?? true;
    public static bool CoparentSupport => Settings?.coparentSupport ?? true;
    public static bool ChildHarmMemories => Settings?.childHarmMemories ?? true;
    public static bool AdoptiveParentWelcomeMemory => Settings?.adoptiveParentWelcomeMemory ?? true;
    public static bool FosterParentWelcomeMemory => Settings?.fosterParentWelcomeMemory ?? true;
    public static bool FosterChildBondMemory => Settings?.fosterChildBondMemory ?? true;

    public static bool IsFamilyPlacementEnabled(ParentKind kind)
    {
        return kind == ParentKind.Foster
            ? FosterQuestsEnabled
            : AdoptionQuestsEnabled;
    }
}
