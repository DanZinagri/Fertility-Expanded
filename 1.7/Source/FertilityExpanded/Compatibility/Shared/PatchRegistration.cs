using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace FertilityExpanded.Patches.Compatibility;

internal static class PatchRegistration
{
    public static void Apply(Harmony harmony)
    {
        if (harmony == null)
        {
            return;
        }

        if (ModDetection.IntimacyActive)
        {
            PatchIntimacy(harmony);
        }

        if (ModDetection.GenderWorksActive)
        {
            PatchGenderWorks(harmony);
        }

        if (ModDetection.RimJobWorldActive)
        {
            PatchRimJobWorld(harmony);
        }

        if (ModDetection.RizrynFamilyTreeActive)
        {
            PatchRizrynFamilyTree(harmony);
        }

        if (ModDetection.FluffyFamilyTreeActive)
        {
            PatchFluffyFamilyTree(harmony);
        }

        if (ModDetection.FamilyIsEverythingActive)
        {
            PatchFamilyIsEverything(harmony);
        }

        if (ModDetection.FamilyTiesActive)
        {
            PatchFamilyTies(harmony);
        }

        if (ModDetection.SizedApparelActive)
        {
            PatchSizedApparel(harmony);
        }

        if (ModDetection.RoosMinotaurActive)
        {
            PatchRoosMinotaur(harmony);
        }

        if (ModDetection.LactationExpansionActive)
        {
            PatchLactationExpansion(harmony);
        }

        if (ModDetection.RomanceOnTheRimActive)
        {
            PatchRomanceOnTheRim(harmony);
        }
    }

    private static void PatchIntimacy(Harmony harmony)
    {
        TryPatchPostfix(
            harmony,
            "LoveyDoveySexWithEuterpe.SexUtilities",
            "DoPregnancy",
            typeof(IntimacyPregnancyPatch),
            nameof(IntimacyPregnancyPatch.Postfix),
            new[] { typeof(Pawn), typeof(Pawn) },
            "Intimacy pregnancy event");

        TryPatchPrefix(
            harmony,
            "LoveyDoveySexWithEuterpe.SexUtilities",
            "ApplyPostBirthHediff",
            typeof(IntimacyRecovery),
            nameof(IntimacyRecovery.ApplyIntimacyRecoveryPrefix),
            new[] { typeof(Pawn) },
            "Intimacy postpartum recovery");
    }

    private static void PatchGenderWorks(Harmony harmony)
    {
        TryPatchPrefixPostfixAndFinalizer(
            harmony,
            "LoveyDoveySexWithRosaline.Hediff_PregnancyEgg",
            "SpawnEgg",
            typeof(GenderWorksCompat),
            nameof(GenderWorksCompat.EggLaidPrefix),
            nameof(GenderWorksCompat.EggLaidPostfix),
            nameof(GenderWorksCompat.EggLaidFinalizer),
            Type.EmptyTypes,
            "GenderWorks automatic egg laying");

        TryPatchPrefixPostfixAndFinalizer(
            harmony,
            "LoveyDoveySexWithRosaline.Hediff_PregnancyEgg",
            "SpawnOrInsertEgg",
            typeof(GenderWorksCompat),
            nameof(GenderWorksCompat.EggLaidPrefix),
            nameof(GenderWorksCompat.EggLaidPostfix),
            nameof(GenderWorksCompat.EggLaidFinalizer),
            Type.EmptyTypes,
            "GenderWorks ordered egg laying");

        TryPatchPrefixAndPostfix(
            harmony,
            "LoveyDoveySexWithRosaline.Comp_HumanEgg",
            "Hatch",
            typeof(GenderWorksCompat),
            nameof(GenderWorksCompat.HatchPrefix),
            nameof(GenderWorksCompat.HatchPostfix),
            new[] { typeof(IntVec3), typeof(Map) },
            "GenderWorks egg hatching");

        TryPatchPostfix(
            harmony,
            typeof(RecipeWorker).FullName,
            nameof(RecipeWorker.IsViolationOnPawn),
            typeof(GenderWorksCompat),
            nameof(GenderWorksCompat.IsViolationPostfix),
            new[] { typeof(Pawn), typeof(BodyPartRecord), typeof(Faction) },
            "GenderWorks egg termination belief");

        TryPatchPrefix(
            harmony,
            typeof(RecipeWorker).FullName,
            "ReportViolation",
            typeof(GenderWorksCompat),
            nameof(GenderWorksCompat.ReportViolationPrefix),
            new[] { typeof(Pawn), typeof(Pawn), typeof(Faction), typeof(int), typeof(HistoryEventDef) },
            "GenderWorks egg termination consequence");
    }

    private static void PatchRimJobWorld(Harmony harmony)
    {
        TryPatchPostfix(
            harmony,
            "rjw.AfterSexUtility",
            "think_about_sex",
            typeof(RjwAfterSexPatch),
            nameof(RjwAfterSexPatch.Postfix),
            null,
            "RJW after-sex event",
            method => method.GetParameters().Length == 1);

        TryPatchPostfix(
            harmony,
            "rjw.Hediff_HumanlikePregnancy",
            "GiveBirth",
            typeof(RjwBirthPatch),
            nameof(RjwBirthPatch.Postfix),
            Type.EmptyTypes,
            "RJW humanlike birth");

        TryPatchPrefixAndPostfix(
            harmony,
            "rjw.Recipe_Abortion",
            nameof(RecipeWorker.ApplyOnPawn),
            typeof(RjwTerminationPatch),
            nameof(RjwTerminationPatch.Prefix),
            nameof(RjwTerminationPatch.Postfix),
            new[] { typeof(Pawn), typeof(BodyPartRecord), typeof(Pawn), typeof(List<Thing>), typeof(Bill) },
            "RJW abortion belief consequence");

        TryPatchPostfix(
            harmony,
            typeof(RecipeWorker).FullName,
            nameof(RecipeWorker.IsViolationOnPawn),
            typeof(RjwTerminationPatch),
            nameof(RjwTerminationPatch.IsViolationPostfix),
            new[] { typeof(Pawn), typeof(BodyPartRecord), typeof(Faction) },
            "RJW abortion belief");

        TryPatchPostfix(
            harmony,
            "rjw.PawnCapacityWorker_Fertility",
            "CalculateCapacityLevel",
            typeof(RjwFertilityPatch),
            nameof(RjwFertilityPatch.Postfix),
            null,
            "RJW fertility capacity",
            method => method.GetParameters().Length == 2
                && method.GetParameters()[0].ParameterType == typeof(HediffSet));
    }

    private static void PatchRizrynFamilyTree(Harmony harmony)
    {
        TryPatchPostfix(
            harmony,
            "FamilyTreeMod.AdoptiveRelationshipsCompat",
            "get_IsActive",
            typeof(RizrynTreeCompat),
            nameof(RizrynTreeCompat.IsActive_Postfix),
            Type.EmptyTypes,
            "Family Tree active state");

        TryPatchPostfix(
            harmony,
            "FamilyTreeMod.AdoptiveRelationshipsCompat",
            "IsAdoptiveRelationDef",
            typeof(RizrynTreeCompat),
            nameof(RizrynTreeCompat.IsAdoptiveRelationDef_Postfix),
            new[] { typeof(PawnRelationDef) },
            "Family Tree relation classification");

        TryPatchPostfix(
            harmony,
            "FamilyTreeMod.AdoptiveRelationshipsCompat",
            "GetAdoptiveParents",
            typeof(RizrynTreeCompat),
            nameof(RizrynTreeCompat.GetAdoptiveParents_Postfix),
            new[] { typeof(Pawn) },
            "Family Tree adoptive parents");

        TryPatchPostfix(
            harmony,
            "FamilyTreeMod.AdoptiveRelationshipsCompat",
            "GetAdoptiveChildren",
            typeof(RizrynTreeCompat),
            nameof(RizrynTreeCompat.GetAdoptiveChildren_Postfix),
            new[] { typeof(Pawn) },
            "Family Tree adoptive children");

        TryPatchPostfix(
            harmony,
            "FamilyTreeMod.AdoptiveRelationshipsCompat",
            "HasAnyAdoptiveFamily",
            typeof(RizrynTreeCompat),
            nameof(RizrynTreeCompat.HasAnyAdoptiveFamily_Postfix),
            new[] { typeof(Pawn) },
            "Family Tree adoptive family detection");

        TryPatchPostfix(
            harmony,
            "FamilyTreeMod.FamilyTreeUtility",
            "GetAdoptiveAncestors",
            typeof(RizrynTreeCompat),
            nameof(RizrynTreeCompat.GetAdoptiveAncestors_Postfix),
            new[] { typeof(Pawn) },
            "Family Tree adoptive ancestors");

        TryPatchPostfix(
            harmony,
            "FamilyTreeMod.FamilyTreeUtility",
            "CalculateAllExtendedAdoptiveRelations",
            typeof(RizrynTreeCompat),
            nameof(RizrynTreeCompat.CalculateAllExtendedAdoptiveRelations_Postfix),
            new[] { typeof(Pawn) },
            "Family Tree adoptive relation labels");

        TryPatchPostfix(
            harmony,
            "FamilyTreeMod.MainTabWindow_FamilyTree",
            "BuildParentChildFamilies",
            typeof(RizrynTreeCompat),
            nameof(RizrynTreeCompat.BuildParentChildFamilies_Postfix),
            new[] { typeof(PawnRelationDef), typeof(bool) },
            "Family Tree parent-child family groups");
    }

    private static void PatchFamilyIsEverything(Harmony harmony)
    {
        if (!FamilyProtectionCompat.Initialize())
        {
            return;
        }

        TryPatchPostfix(
            harmony,
            "CrycoreFamilyisEverything.RitualRoleAdoptee",
            nameof(RitualRole.AppliesToPawn),
            typeof(FamilyProtectionCompat),
            nameof(FamilyProtectionCompat.AdopteeAppliesToPawnPostfix),
            new[] { typeof(Pawn), typeof(string).MakeByRefType(), typeof(TargetInfo), typeof(LordJob_Ritual), typeof(RitualRoleAssignments), typeof(Precept_Ritual), typeof(bool) },
            "Family Is Everything adoption eligibility");

        TryPatchPrefixAndPostfix(
            harmony,
            "CrycoreFamilyisEverything.RitualOutcomeEffectWorker_Adoption",
            nameof(RitualOutcomeEffectWorker.Apply),
            typeof(FamilyProtectionCompat),
            nameof(FamilyProtectionCompat.AdoptionApplyPrefix),
            nameof(FamilyProtectionCompat.AdoptionApplyPostfix),
            new[] { typeof(float), typeof(Dictionary<Pawn, int>), typeof(LordJob_Ritual) },
            "Family Is Everything adoption outcome");

        TryPatchPostfix(
            harmony,
            "CrycoreFamilyisEverything.PostPreApplyDamage_Patch",
            "MakeFamilyMembersKnown",
            typeof(FamilyProtectionCompat),
            nameof(FamilyProtectionCompat.MakeFamilyMembersKnownPostfix),
            new[] { typeof(Pawn), typeof(Pawn) },
            "Family Is Everything family response");
    }

    private static void PatchRomanceOnTheRim(Harmony harmony)
    {
        TryPatchPrefix(
            harmony,
            "RomanceOnTheRim.HarmonyPatch_TriggerAdoptQuestAtBirthday",
            "CheckAdoptionChance",
            typeof(RomanceCompat),
            nameof(RomanceCompat.AdoptionChancePrefix),
            new[] { typeof(Pawn), typeof(int) },
            "Romance On The Rim adoption-chance ownership");

        TryPatchPostfix(
            harmony,
            "RomanceOnTheRim.QuestPart_OrphanLover_Accept",
            "DoAction",
            typeof(RomanceCompat),
            nameof(RomanceCompat.OrphanLoverAcceptedPostfix),
            Type.EmptyTypes,
            "Romance On The Rim orphan-lover family placement");
    }

    private static void PatchFluffyFamilyTree(Harmony harmony)
    {
        if (!FluffyTreeCompat.Initialize())
        {
            return;
        }

        TryPatchPostfix(
            harmony,
            "FamilyTree.RelationsHelper",
            "VisibleInTab",
            typeof(FluffyTreeCompat),
            nameof(FluffyTreeCompat.VisibleInTabPostfix),
            new[] { typeof(PawnRelationDef) },
            "Fluffy Family Tree relation visibility");

        TryPatchPostfix(
            harmony,
            "FamilyTree.FamilyTreeMap.FamilyMember",
            "CreateParentRelationships",
            typeof(FluffyTreeCompat),
            nameof(FluffyTreeCompat.CreateParentRelationshipsPostfix),
            null,
            "Fluffy Family Tree parent-child relationships",
            method => method.GetParameters().Length == 1);
    }

    private static void PatchFamilyTies(Harmony harmony)
    {
        if (!FamilyTiesCompat.Initialize())
        {
            return;
        }

        TryPatchPostfix(
            harmony,
            typeof(Thing).FullName,
            nameof(Thing.TakeDamage),
            typeof(FamilyTiesCompat),
            nameof(FamilyTiesCompat.TakeDamagePostfix),
            new[] { typeof(DamageInfo) },
            "Family Ties child-harm memories");

        TryPatchPostfix(
            harmony,
            typeof(Pawn_HealthTracker).FullName,
            nameof(Pawn_HealthTracker.AddHediff),
            typeof(FamilyTiesCompat),
            nameof(FamilyTiesCompat.AddHediffPostfix),
            new[] { typeof(Hediff), typeof(BodyPartRecord), typeof(DamageInfo?), typeof(DamageWorker.DamageResult) },
            "Family Ties limb-loss memories");

        TryPatchPostfix(
            harmony,
            "FamilyTies.MasterpieceUtil",
            "CheckAndRewardParent",
            typeof(FamilyTiesCompat),
            nameof(FamilyTiesCompat.MasterpiecePostfix),
            new[] { typeof(Pawn), typeof(Thing) },
            "Family Ties masterpiece memories");

        TryPatchPrefixAndPostfix(
            harmony,
            typeof(SkillRecord).FullName,
            nameof(SkillRecord.Learn),
            typeof(FamilyTiesCompat),
            nameof(FamilyTiesCompat.SkillLearnPrefix),
            nameof(FamilyTiesCompat.SkillLearnPostfix),
            new[] { typeof(float), typeof(bool), typeof(bool) },
            "Family Ties skill memories");

        PatchFamilyTiesThoughtWorker(
            harmony,
            "FamilyTies.ThoughtWorker_MyChildIsInPain",
            nameof(FamilyTiesCompat.MyChildIsInPainPostfix),
            "Family Ties child pain thought");
        PatchFamilyTiesThoughtWorker(
            harmony,
            "FamilyTies.ThoughtWorker_MyChildIsSick",
            nameof(FamilyTiesCompat.MyChildIsSickPostfix),
            "Family Ties child sickness thought");
        PatchFamilyTiesThoughtWorker(
            harmony,
            "FamilyTies.ThoughtWorker_MyParentIsInPain",
            nameof(FamilyTiesCompat.MyParentIsInPainPostfix),
            "Family Ties parent pain thought");
        PatchFamilyTiesThoughtWorker(
            harmony,
            "FamilyTies.ThoughtWorker_MyParentIsSick",
            nameof(FamilyTiesCompat.MyParentIsSickPostfix),
            "Family Ties parent sickness thought");

        PatchFamilyTiesThoughtHelper(
            harmony,
            "FamilyTies.Thought_MyChildIsInPain",
            "FindFirstSufferingChild",
            nameof(FamilyTiesCompat.FindFirstSufferingChildPostfix),
            "Family Ties child pain label");
        PatchFamilyTiesThoughtHelper(
            harmony,
            "FamilyTies.Thought_MyChildIsSick",
            "FindSingleSickChild",
            nameof(FamilyTiesCompat.FindSingleSickChildPostfix),
            "Family Ties child sickness text");
        PatchFamilyTiesThoughtHelper(
            harmony,
            "FamilyTies.Thought_MyParentIsInPain",
            "FindSingleSufferingParent",
            nameof(FamilyTiesCompat.FindSingleSufferingParentPostfix),
            "Family Ties parent pain text");
        PatchFamilyTiesThoughtHelper(
            harmony,
            "FamilyTies.Thought_MyParentIsSick",
            "FindSingleSickParent",
            nameof(FamilyTiesCompat.FindSingleSickParentPostfix),
            "Family Ties parent sickness text");
    }

    private static void PatchFamilyTiesThoughtWorker(Harmony harmony, string typeName, string postfixName, string label)
    {
        TryPatchPostfix(
            harmony,
            typeName,
            "CurrentStateInternal",
            typeof(FamilyTiesCompat),
            postfixName,
            new[] { typeof(Pawn) },
            label);
    }

    private static void PatchFamilyTiesThoughtHelper(
        Harmony harmony,
        string typeName,
        string methodName,
        string postfixName,
        string label)
    {
        TryPatchPostfix(
            harmony,
            typeName,
            methodName,
            typeof(FamilyTiesCompat),
            postfixName,
            Type.EmptyTypes,
            label);
    }

    private static void PatchSizedApparel(Harmony harmony)
    {
        if (!SizedApparelCompat.Initialize())
        {
            return;
        }

        TryPatchPrefixAndPostfix(
            harmony,
            "SizedApparel.ApparelRecorderComp",
            "Update",
            typeof(SizedApparelCompat),
            nameof(SizedApparelCompat.UpdatePrefix),
            nameof(SizedApparelCompat.UpdatePostfix),
            new[] { typeof(bool), typeof(bool), typeof(bool), typeof(bool) },
            "Sized Apparel lactation size cache");

        TryPatchPrefix(
            harmony,
            "SizedApparel.SizedApparelBodyPart",
            "SetHediffData",
            typeof(SizedApparelCompat),
            nameof(SizedApparelCompat.SetHediffDataPrefix),
            new[] { typeof(string), typeof(int), typeof(int), typeof(string) },
            "Sized Apparel lactation body rendering");

        TryPatchPostfix(
            harmony,
            "SizedApparel.SizedApparelUtility",
            "SetHediffDirty",
            typeof(SizedApparelCompat),
            nameof(SizedApparelCompat.SetHediffDirtyPostfix),
            null,
            "Sized Apparel lactation refresh",
            method =>
            {
                ParameterInfo[] parameters = method.GetParameters();
                return parameters.Length == 2 && parameters[1].ParameterType == typeof(Hediff);
            });

        TryPatchPostfix(
            harmony,
            "SizedApparel.HediffPostRemovedPatch",
            "Postfix",
            typeof(SizedApparelCompat),
            nameof(SizedApparelCompat.LactationRemovedPostfix),
            new[] { typeof(Hediff).MakeByRefType() },
            "Sized Apparel lactation removal refresh");
    }

    private static void PatchLactationExpansion(Harmony harmony)
    {
        TryPatchPostfix(
            harmony,
            "EuterpeMilkyTitfuck.LactationUtilities",
            "GenerateMilk",
            typeof(LactationExpansionCompat),
            nameof(LactationExpansionCompat.GenerateMilkPostfix),
            new[] { typeof(Pawn) },
            "Lactation Expansion produced milk");

        TryPatchPostfix(
            harmony,
            "EuterpeMilkyTitfuck.HediffComp_Lactation",
            "RecacheStats",
            typeof(LactationExpansionCompat),
            nameof(LactationExpansionCompat.RecacheStatsPostfix),
            Type.EmptyTypes,
            "Lactation Expansion breast size scaling");
    }

    private static void PatchRoosMinotaur(Harmony harmony)
    {
        MinotaurCompat.Initialize();

        TryPatchPostfix(
            harmony,
            typeof(PawnGenerator).FullName,
            "GenerateNewPawnInternal",
            typeof(MinotaurCompat),
            nameof(MinotaurCompat.GeneratedPawnPostfix),
            new[] { typeof(PawnGenerationRequest).MakeByRefType() },
            "Roo's Minotaur generated-pawn lactation");

        TryPatchPostfix(
            harmony,
            "RBM_Minotaur.CompAbilityEffect_SpawnStack",
            "get_CanCast",
            typeof(MinotaurCompat),
            nameof(MinotaurCompat.CanCastPostfix),
            Type.EmptyTypes,
            "Roo's Minotaur lactation ability availability");

        TryPatchPostfix(
            harmony,
            "RBM_Minotaur.CompAbilityEffect_SpawnStack",
            "get_ShouldHideGizmo",
            typeof(MinotaurCompat),
            nameof(MinotaurCompat.ShouldHideGizmoPostfix),
            Type.EmptyTypes,
            "Roo's Minotaur lactation ability visibility");

        TryPatchPrefix(
            harmony,
            "RBM_Minotaur.CompAbilityEffect_SpawnStack",
            nameof(CompAbilityEffect.Apply),
            typeof(MinotaurCompat),
            nameof(MinotaurCompat.ApplyPrefix),
            new[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo) },
            "Roo's Minotaur lactation ability output");
    }

    private static void TryPatchPostfix(
        Harmony harmony,
        string typeName,
        string methodName,
        Type patchType,
        string patchMethodName,
        Type[] parameterTypes,
        string label,
        Func<MethodInfo, bool> methodPredicate = null)
    {
        TryPatch(
            harmony,
            typeName,
            methodName,
            patchType,
            parameterTypes,
            label,
            null,
            patchMethodName,
            methodPredicate);
    }

    private static void TryPatchPrefix(
        Harmony harmony,
        string typeName,
        string methodName,
        Type patchType,
        string patchMethodName,
        Type[] parameterTypes,
        string label,
        Func<MethodInfo, bool> methodPredicate = null)
    {
        TryPatch(
            harmony,
            typeName,
            methodName,
            patchType,
            parameterTypes,
            label,
            patchMethodName,
            null,
            methodPredicate);
    }

    private static void TryPatchPrefixAndPostfix(
        Harmony harmony,
        string typeName,
        string methodName,
        Type patchType,
        string prefixPatchMethodName,
        string postfixPatchMethodName,
        Type[] parameterTypes,
        string label,
        Func<MethodInfo, bool> methodPredicate = null)
    {
        TryPatch(
            harmony,
            typeName,
            methodName,
            patchType,
            parameterTypes,
            label,
            prefixPatchMethodName,
            postfixPatchMethodName,
            methodPredicate);
    }

    private static void TryPatchPrefixPostfixAndFinalizer(
        Harmony harmony,
        string typeName,
        string methodName,
        Type patchType,
        string prefixPatchMethodName,
        string postfixPatchMethodName,
        string finalizerPatchMethodName,
        Type[] parameterTypes,
        string label,
        Func<MethodInfo, bool> methodPredicate = null)
    {
        TryPatch(
            harmony,
            typeName,
            methodName,
            patchType,
            parameterTypes,
            label,
            prefixPatchMethodName,
            postfixPatchMethodName,
            methodPredicate,
            finalizerPatchMethodName);
    }

    private static void TryPatch(
        Harmony harmony,
        string typeName,
        string methodName,
        Type patchType,
        Type[] parameterTypes,
        string label,
        string prefixPatchMethodName,
        string postfixPatchMethodName,
        Func<MethodInfo, bool> methodPredicate,
        string finalizerPatchMethodName = null)
    {
        Type originalType = AccessTools.TypeByName(typeName);
        if (originalType == null)
        {
            Log.Warning("[Fertility Expanded] Could not find " + typeName + " for " + label + " compatibility.");
            return;
        }

        MethodInfo original = parameterTypes == null
            ? FirstMatchingMethod(originalType, methodName, methodPredicate)
            : AccessTools.Method(originalType, methodName, parameterTypes);

        if (original == null)
        {
            Log.Warning("[Fertility Expanded] Could not find " + typeName + "." + methodName + " for " + label + " compatibility.");
            return;
        }

        MethodInfo prefixPatch = prefixPatchMethodName == null
            ? null
            : AccessTools.Method(patchType, prefixPatchMethodName);
        MethodInfo postfixPatch = postfixPatchMethodName == null
            ? null
            : AccessTools.Method(patchType, postfixPatchMethodName);
        MethodInfo finalizerPatch = finalizerPatchMethodName == null
            ? null
            : AccessTools.Method(patchType, finalizerPatchMethodName);

        if (prefixPatchMethodName != null && prefixPatch == null)
        {
            Log.Error("[Fertility Expanded] Missing compatibility patch method " + patchType.FullName + "." + prefixPatchMethodName + ".");
            return;
        }

        if (postfixPatchMethodName != null && postfixPatch == null)
        {
            Log.Error("[Fertility Expanded] Missing compatibility patch method " + patchType.FullName + "." + postfixPatchMethodName + ".");
            return;
        }

        if (finalizerPatchMethodName != null && finalizerPatch == null)
        {
            Log.Error("[Fertility Expanded] Missing compatibility patch method " + patchType.FullName + "." + finalizerPatchMethodName + ".");
            return;
        }

        harmony.Patch(
            original,
            prefix: prefixPatch == null ? null : new HarmonyMethod(prefixPatch),
            postfix: postfixPatch == null ? null : new HarmonyMethod(postfixPatch),
            finalizer: finalizerPatch == null ? null : new HarmonyMethod(finalizerPatch));
    }

    private static MethodInfo FirstMatchingMethod(Type type, string methodName, Func<MethodInfo, bool> predicate)
    {
        var methods = AccessTools.GetDeclaredMethods(type);
        for (int i = 0; i < methods.Count; i++)
        {
            MethodInfo method = methods[i];
            if (method.Name == methodName && (predicate == null || predicate(method)))
            {
                return method;
            }
        }

        return null;
    }
}
