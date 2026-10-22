using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace FertilityExpanded.Patches.Compatibility;

internal static class SizedApparelCompat
{
    private const float TierBoundaryEpsilon = 0.0001f;
    private const int LactationCheckIntervalTicks = 60;

    private static readonly ConditionalWeakTable<object, VisualState> states = new ConditionalWeakTable<object, VisualState>();

    private static Type apparelRecorderCompType;
    private static FieldInfo apparelRecorderBreastSeverityField;
    private static FieldInfo apparelRecorderBreastHediffsField;
    private static FieldInfo apparelRecorderBreastDirtyField;
    private static FieldInfo apparelRecorderHediffDirtyField;
    private static FieldInfo apparelRecorderDirtyField;
    private static FieldInfo bodyPartIsBreastField;
    private static FieldInfo bodyPartPawnField;
    private static FieldInfo bodyPartRecorderField;
    private static Func<float, int> breastSeverityToIndex;
    private static Func<int, float> breastIndexToSeverity;
    private static bool initialized;
    private static bool available;

    public static bool Initialize()
    {
        if (initialized)
        {
            return available;
        }

        initialized = true;
        if (!ModDetection.SizedApparelActive)
        {
            return false;
        }

        apparelRecorderCompType = AccessTools.TypeByName("SizedApparel.ApparelRecorderComp");
        Type bodyPartType = AccessTools.TypeByName("SizedApparel.SizedApparelBodyPart");
        Type utilityType = AccessTools.TypeByName("SizedApparel.SizedApparelUtility");
        Type removalPatchType = AccessTools.TypeByName("SizedApparel.HediffPostRemovedPatch");
        if (apparelRecorderCompType == null || bodyPartType == null || utilityType == null || removalPatchType == null)
        {
            Log.Warning("[Fertility Expanded] Sized Apparel is active, but its visual compatibility types were not found.");
            return false;
        }

        apparelRecorderBreastSeverityField = AccessTools.Field(apparelRecorderCompType, "breastSeverity");
        apparelRecorderBreastHediffsField = AccessTools.Field(apparelRecorderCompType, "breastHediffs");
        apparelRecorderBreastDirtyField = AccessTools.Field(apparelRecorderCompType, "breastHediff_Dirty");
        apparelRecorderHediffDirtyField = AccessTools.Field(apparelRecorderCompType, "isHediffDirty");
        apparelRecorderDirtyField = AccessTools.Field(apparelRecorderCompType, "isDirty");
        bodyPartIsBreastField = AccessTools.Field(bodyPartType, "isBreast");
        bodyPartPawnField = AccessTools.Field(bodyPartType, "pawn");
        bodyPartRecorderField = AccessTools.Field(bodyPartType, "apparelRecorderCompCache");
        MethodInfo severityToIndexMethod = AccessTools.Method(utilityType, "BreastSeverityInt", new[] { typeof(float) });
        MethodInfo indexToSeverityMethod = AccessTools.Method(utilityType, "BreastSizeIndexToSeverity", new[] { typeof(int) });
        MethodInfo removalMethod = AccessTools.Method(removalPatchType, "Postfix", new[] { typeof(Hediff).MakeByRefType() });

        if (apparelRecorderBreastSeverityField?.FieldType != typeof(float)
            || apparelRecorderBreastHediffsField == null
            || apparelRecorderBreastDirtyField?.FieldType != typeof(bool)
            || apparelRecorderHediffDirtyField?.FieldType != typeof(bool)
            || apparelRecorderDirtyField?.FieldType != typeof(bool)
            || bodyPartIsBreastField?.FieldType != typeof(bool)
            || bodyPartPawnField == null
            || bodyPartRecorderField == null
            || severityToIndexMethod?.ReturnType != typeof(int)
            || indexToSeverityMethod?.ReturnType != typeof(float)
            || removalMethod == null)
        {
            Log.Warning("[Fertility Expanded] Sized Apparel is active, but its visual compatibility members do not match the supported API.");
            return false;
        }

        breastSeverityToIndex = (Func<float, int>)Delegate.CreateDelegate(typeof(Func<float, int>), severityToIndexMethod);
        breastIndexToSeverity = (Func<int, float>)Delegate.CreateDelegate(typeof(Func<int, float>), indexToSeverityMethod);
        available = true;
        return true;
    }

    public static void UpdatePrefix(object __instance, out bool __state)
    {
        __state = false;
        if (!available || __instance == null)
        {
            return;
        }

        VisualState state = states.GetOrCreateValue(__instance);
        __state = ReadBool(apparelRecorderHediffDirtyField, __instance)
            && ReadBool(apparelRecorderBreastDirtyField, __instance);
        state.RecalculatingBreasts = __state;
    }

    public static void UpdatePostfix(object __instance, bool __state)
    {
        if (!available || __instance == null)
        {
            return;
        }

        VisualState state = states.GetOrCreateValue(__instance);
        bool completedBreastRecalculation = __state && !ReadBool(apparelRecorderBreastDirtyField, __instance);
        bool hasBreasts = HasRecordedBreasts(__instance);

        if (completedBreastRecalculation || (!state.Initialized && hasBreasts))
        {
            state.BaseSeverity = ReadFloat(apparelRecorderBreastSeverityField, __instance);
            state.Initialized = true;
        }

        state.HasBreasts = hasBreasts;
        state.RecalculatingBreasts = false;
        state.RefreshRequested = false;

        if (!state.Initialized || !hasBreasts)
        {
            state.RequestedVisualTier = -1;
            return;
        }

        Pawn pawn = (__instance as ThingComp)?.parent as Pawn;
        float visualSeverity = CalculateVisualSeverity(state.BaseSeverity, LactationFactor(pawn));
        apparelRecorderBreastSeverityField.SetValue(__instance, visualSeverity);
        state.RequestedVisualTier = breastSeverityToIndex(visualSeverity);
    }

    public static void SetHediffDataPrefix(object __instance, ref int __1)
    {
        if (!available
            || !SettingEnabled
            || __instance == null
            || __1 < 0
            || !ReadBool(bodyPartIsBreastField, __instance))
        {
            return;
        }

        Pawn pawn = bodyPartPawnField.GetValue(__instance) as Pawn;
        float lactationFactor = LactationFactor(pawn);
        if (lactationFactor <= 0f)
        {
            return;
        }

        object recorder = bodyPartRecorderField.GetValue(__instance);
        if (recorder == null)
        {
            return;
        }

        VisualState state = states.GetOrCreateValue(recorder);
        float baseSeverity = state.RecalculatingBreasts || !state.Initialized
            ? ReadFloat(apparelRecorderBreastSeverityField, recorder)
            : state.BaseSeverity;
        int recorderBaseTier = breastSeverityToIndex(baseSeverity);
        bool usesRecorderSeverity = __1 == recorderBaseTier
            || (!state.RecalculatingBreasts
                && state.Initialized
                && __1 == state.RequestedVisualTier);
        int naturalTier = usesRecorderSeverity ? recorderBaseTier : __1;
        if (!usesRecorderSeverity)
        {
            baseSeverity = TierMidpoint(__1);
        }

        int visualTier = breastSeverityToIndex(CalculateVisualSeverity(baseSeverity, lactationFactor));
        __1 = Math.Max(naturalTier, Math.Min(naturalTier + 1, visualTier));
    }

    public static void SetHediffDirtyPostfix(object __0, Hediff __1)
    {
        if (!available
            || !SettingEnabled
            || !ModsConfig.BiotechActive
            || __0 == null
            || __1?.def != HediffDefOf.Lactating)
        {
            return;
        }

        VisualState state = states.GetOrCreateValue(__0);
        if (state.RefreshRequested)
        {
            return;
        }

        if (!state.Initialized)
        {
            RequestRefresh(__0, state);
            return;
        }

        if (!state.HasBreasts)
        {
            return;
        }

        int ticksGame = Find.TickManager?.TicksGame ?? -1;
        if (__1.Severity > 0f
            && ticksGame >= 0
            && state.LastLactationCheckTick >= 0
            && ticksGame - state.LastLactationCheckTick < LactationCheckIntervalTicks)
        {
            return;
        }

        state.LastLactationCheckTick = ticksGame;
        float lactationFactor = Mathf.Clamp01(__1.Severity);
        int visualTier = breastSeverityToIndex(CalculateVisualSeverity(state.BaseSeverity, lactationFactor));
        if (visualTier == state.RequestedVisualTier)
        {
            return;
        }

        state.RequestedVisualTier = visualTier;
        RequestRefresh(__0, state);
    }

    public static void LactationRemovedPostfix(Hediff __0)
    {
        if (!available || !SettingEnabled || __0?.def != HediffDefOf.Lactating)
        {
            return;
        }

        List<ThingComp> comps = __0.pawn?.AllComps;
        if (comps == null)
        {
            return;
        }

        for (int i = 0; i < comps.Count; i++)
        {
            ThingComp comp = comps[i];
            if (comp != null && apparelRecorderCompType.IsInstanceOfType(comp))
            {
                if (states.TryGetValue(comp, out VisualState state))
                {
                    state.LastLactationCheckTick = -1;
                    RequestRefresh(comp, state);
                }
                return;
            }
        }
    }

    public static void RefreshAllPawns()
    {
        if (!available || Current.Game == null)
        {
            return;
        }

        List<Pawn> pawns = PawnsFinder.AllMapsWorldAndTemporary_Alive;
        for (int i = 0; i < pawns.Count; i++)
        {
            Pawn pawn = pawns[i];
            List<ThingComp> comps = pawn?.AllComps;
            if (comps == null)
            {
                continue;
            }

            for (int j = 0; j < comps.Count; j++)
            {
                ThingComp comp = comps[j];
                if (comp == null || !apparelRecorderCompType.IsInstanceOfType(comp))
                {
                    continue;
                }

                states.Remove(comp);
                VisualState state = states.GetOrCreateValue(comp);
                RequestRefresh(comp, state);
                break;
            }
        }
    }

    private static bool SettingEnabled => FertilityExpandedMod.Settings?.sizedApparelLactationGrowth ?? true;

    private static float LactationFactor(Pawn pawn)
    {
        if (!SettingEnabled || !ModsConfig.BiotechActive || pawn?.health?.hediffSet == null)
        {
            return 0f;
        }

        Hediff lactating = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Lactating);
        return Mathf.Clamp01(lactating?.Severity ?? 0f);
    }

    private static float CalculateVisualSeverity(float baseSeverity, float lactationFactor)
    {
        if (lactationFactor <= 0f || baseSeverity < 0f)
        {
            return baseSeverity;
        }

        int baseTier = breastSeverityToIndex(baseSeverity);
        float currentTierMinimum = breastIndexToSeverity(baseTier);
        float nextTierMinimum = breastIndexToSeverity(baseTier + 1);
        if (nextTierMinimum <= currentTierMinimum)
        {
            return baseSeverity;
        }

        float tierWidth = nextTierMinimum - currentTierMinimum;
        float fullSeverityTarget = Math.Max(nextTierMinimum, baseSeverity + tierWidth);
        float visualSeverity = Mathf.Lerp(baseSeverity, fullSeverityTarget, lactationFactor);
        float followingTierMinimum = breastIndexToSeverity(baseTier + 2);
        if (followingTierMinimum > nextTierMinimum)
        {
            visualSeverity = Math.Min(visualSeverity, followingTierMinimum - TierBoundaryEpsilon);
        }

        return visualSeverity;
    }

    private static float TierMidpoint(int tier)
    {
        float currentTierMinimum = breastIndexToSeverity(tier);
        float nextTierMinimum = breastIndexToSeverity(tier + 1);
        if (nextTierMinimum <= currentTierMinimum)
        {
            return currentTierMinimum;
        }

        return (currentTierMinimum + nextTierMinimum) * 0.5f;
    }

    private static void RequestRefresh(object recorder, VisualState state)
    {
        if (state.RefreshRequested)
        {
            return;
        }

        state.RefreshRequested = true;
        apparelRecorderBreastDirtyField.SetValue(recorder, true);
        apparelRecorderHediffDirtyField.SetValue(recorder, true);
        apparelRecorderDirtyField.SetValue(recorder, true);
    }

    private static bool HasRecordedBreasts(object recorder)
    {
        return apparelRecorderBreastHediffsField.GetValue(recorder) is ICollection hediffs
            && hediffs.Count > 0;
    }

    private static bool ReadBool(FieldInfo field, object instance)
    {
        return field.GetValue(instance) is bool value && value;
    }

    private static float ReadFloat(FieldInfo field, object instance)
    {
        return field.GetValue(instance) is float value ? value : -1f;
    }

    private sealed class VisualState
    {
        public float BaseSeverity = -1f;
        public int RequestedVisualTier = -1;
        public int LastLactationCheckTick = -1;
        public bool Initialized;
        public bool HasBreasts;
        public bool RecalculatingBreasts;
        public bool RefreshRequested;
    }
}
