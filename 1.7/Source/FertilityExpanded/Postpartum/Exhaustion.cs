using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace FertilityExpanded;

public class PostpartumExhaustion : HediffWithComps
{
    private const int UnknownOverrideMode = -1;
    private const int VanillaOverrideMode = 0;
    private const int FertilityExpandedOverrideMode = 1;
    private const int FertilityExpandedDurationTicks = 60000;
    private const float FertilityExpandedSeverityPerDay = 0.5f;
    private const float FertilityExpandedMovingFactor = 0.6f;

    private static readonly MethodInfo MemberwiseCloneMethod = AccessTools.Method(typeof(object), "MemberwiseClone");

    private int appliedOverrideMode = UnknownOverrideMode;

    [Unsaved]
    private List<HediffStage> fertilityExpandedStages;

    private bool UsesFertilityExpandedOverride
    {
        get
        {
            if (appliedOverrideMode == UnknownOverrideMode)
            {
                appliedOverrideMode = InferLegacyOverrideMode();
            }

            return appliedOverrideMode == FertilityExpandedOverrideMode;
        }
    }

    public override HediffStage CurStage
    {
        get
        {
            if (!UsesFertilityExpandedOverride || def?.stages.NullOrEmpty() != false)
            {
                return base.CurStage;
            }

            fertilityExpandedStages ??= CreateFertilityExpandedStages(def.stages);
            int stageIndex = CurStageIndex;
            return stageIndex >= 0 && stageIndex < fertilityExpandedStages.Count
                ? fertilityExpandedStages[stageIndex]
                : base.CurStage;
        }
    }

    public override void PostMake()
    {
        appliedOverrideMode = Features.PostpartumExhaustionOverride
            ? FertilityExpandedOverrideMode
            : VanillaOverrideMode;
        base.PostMake();
        ApplyFertilityExpandedCompValues();
    }

    public override void PostAdd(DamageInfo? dinfo)
    {
        base.PostAdd(dinfo);
        ApplyFertilityExpandedCompValues();
    }

    public override bool TryMergeWith(Hediff other)
    {
        bool merged = base.TryMergeWith(other);
        if (!merged || other is not PostpartumExhaustion otherPostpartum)
        {
            return merged;
        }

        appliedOverrideMode = otherPostpartum.UsesFertilityExpandedOverride
            ? FertilityExpandedOverrideMode
            : VanillaOverrideMode;
        fertilityExpandedStages = null;
        CopyAppliedCompValues(otherPostpartum);
        return true;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(
            ref appliedOverrideMode,
            "fertilityExpandedPostpartumExhaustionOverride",
            UnknownOverrideMode);

        if (Scribe.mode == LoadSaveMode.PostLoadInit && appliedOverrideMode == UnknownOverrideMode)
        {
            appliedOverrideMode = InferLegacyOverrideMode();
        }
    }

    internal static void MigrateLegacyInstances()
    {
        if (Current.Game == null
            || HediffDefOf.PostpartumExhaustion?.hediffClass != typeof(PostpartumExhaustion))
        {
            return;
        }

        List<Pawn> pawns = PawnsFinder.All_AliveOrDead;
        for (int pawnIndex = 0; pawnIndex < pawns.Count; pawnIndex++)
        {
            Pawn pawn = pawns[pawnIndex];
            List<Hediff> hediffs = pawn?.health?.hediffSet?.hediffs;
            if (hediffs == null)
            {
                continue;
            }

            bool migrated = false;
            for (int hediffIndex = 0; hediffIndex < hediffs.Count; hediffIndex++)
            {
                Hediff legacy = hediffs[hediffIndex];
                if (legacy?.def != HediffDefOf.PostpartumExhaustion
                    || legacy.GetType() != typeof(HediffWithComps))
                {
                    continue;
                }

                Rand.PushState();
                PostpartumExhaustion replacement;
                try
                {
                    replacement = new PostpartumExhaustion
                    {
                        def = legacy.def,
                        pawn = pawn,
                        loadID = legacy.loadID
                    };
                    replacement.Part = legacy.Part;
                    replacement.PostMake();
                }
                finally
                {
                    Rand.PopState();
                }

                replacement.CopyLegacyState(legacy);
                hediffs[hediffIndex] = replacement;
                migrated = true;
            }

            if (migrated)
            {
                pawn.health.hediffSet.DirtyCache();
            }
        }
    }

    private void ApplyFertilityExpandedCompValues()
    {
        if (!UsesFertilityExpandedOverride)
        {
            return;
        }

        HediffComp_Disappears disappears = GetComp<HediffComp_Disappears>();
        disappears?.SetDuration(FertilityExpandedDurationTicks);

        HediffComp_SeverityPerDay severityPerDay = GetComp<HediffComp_SeverityPerDay>();
        if (severityPerDay != null)
        {
            severityPerDay.severityPerDay = FertilityExpandedSeverityPerDay;
        }
    }

    private void CopyAppliedCompValues(PostpartumExhaustion source)
    {
        HediffComp_Disappears sourceDisappears = source.GetComp<HediffComp_Disappears>();
        HediffComp_Disappears disappears = GetComp<HediffComp_Disappears>();
        if (sourceDisappears != null && disappears != null)
        {
            disappears.SetDuration(sourceDisappears.disappearsAfterTicks);
        }

        HediffComp_SeverityPerDay sourceSeverity = source.GetComp<HediffComp_SeverityPerDay>();
        HediffComp_SeverityPerDay severity = GetComp<HediffComp_SeverityPerDay>();
        if (sourceSeverity != null && severity != null)
        {
            HediffCompProperties_SeverityPerDay sourceProps = sourceSeverity.props as HediffCompProperties_SeverityPerDay;
            severity.severityPerDay = source.UsesFertilityExpandedOverride
                ? FertilityExpandedSeverityPerDay
                : sourceProps?.CalculateSeverityPerDay() ?? sourceSeverity.severityPerDay;
        }
    }

    private int InferLegacyOverrideMode()
    {
        return InferOverrideMode(
            GetComp<HediffComp_Disappears>(),
            GetComp<HediffComp_SeverityPerDay>());
    }

    private void CopyLegacyState(Hediff legacy)
    {
        CopyFrom(legacy);
        tickAdded = legacy.tickAdded;
        loadID = legacy.loadID;
        sourceToolLabel = legacy.sourceToolLabel;
        sourceHediffDef = legacy.sourceHediffDef;
        canBeThreateningToPart = legacy.canBeThreateningToPart;
        combatLogEntry = legacy.combatLogEntry;
        combatLogText = legacy.combatLogText;

        HediffComp_Disappears legacyDisappears = legacy.TryGetComp<HediffComp_Disappears>();
        HediffComp_Disappears disappears = GetComp<HediffComp_Disappears>();
        if (legacyDisappears != null && disappears != null)
        {
            disappears.ticksToDisappear = legacyDisappears.ticksToDisappear;
            disappears.disappearsAfterTicks = legacyDisappears.disappearsAfterTicks;
            disappears.seed = legacyDisappears.seed;
        }

        HediffComp_SeverityPerDay legacySeverity = legacy.TryGetComp<HediffComp_SeverityPerDay>();
        HediffComp_SeverityPerDay severity = GetComp<HediffComp_SeverityPerDay>();
        if (legacySeverity != null && severity != null)
        {
            severity.severityPerDay = legacySeverity.severityPerDay;
        }

        appliedOverrideMode = InferOverrideMode(legacyDisappears, legacySeverity);
        fertilityExpandedStages = null;
    }

    private static int InferOverrideMode(
        HediffComp_Disappears disappears,
        HediffComp_SeverityPerDay severityPerDay)
    {
        if (disappears?.disappearsAfterTicks > 0 || severityPerDay?.severityPerDay != 0f)
        {
            return disappears?.disappearsAfterTicks == FertilityExpandedDurationTicks
                && Math.Abs((severityPerDay?.severityPerDay ?? 0f) - FertilityExpandedSeverityPerDay) < 0.0001f
                    ? FertilityExpandedOverrideMode
                    : VanillaOverrideMode;
        }

        return Features.PostpartumExhaustionOverride
            ? FertilityExpandedOverrideMode
            : VanillaOverrideMode;
    }

    private static List<HediffStage> CreateFertilityExpandedStages(List<HediffStage> sourceStages)
    {
        List<HediffStage> stages = new List<HediffStage>(sourceStages.Count);
        for (int i = 0; i < sourceStages.Count; i++)
        {
            HediffStage sourceStage = sourceStages[i];
            HediffStage stage = (HediffStage)MemberwiseCloneMethod.Invoke(sourceStage, null);
            stage.capMods = CopyCapacityModifiers(sourceStage.capMods);

            if (i == 1 && stage.capMods != null)
            {
                for (int modifierIndex = 0; modifierIndex < stage.capMods.Count; modifierIndex++)
                {
                    PawnCapacityModifier modifier = stage.capMods[modifierIndex];
                    if (modifier?.capacity == PawnCapacityDefOf.Moving)
                    {
                        modifier.postFactor = FertilityExpandedMovingFactor;
                    }
                }
            }

            stages.Add(stage);
        }

        return stages;
    }

    private static List<PawnCapacityModifier> CopyCapacityModifiers(List<PawnCapacityModifier> source)
    {
        if (source == null)
        {
            return null;
        }

        List<PawnCapacityModifier> copy = new List<PawnCapacityModifier>(source.Count);
        for (int i = 0; i < source.Count; i++)
        {
            PawnCapacityModifier modifier = source[i];
            copy.Add(modifier == null
                ? null
                : new PawnCapacityModifier
                {
                    capacity = modifier.capacity,
                    offset = modifier.offset,
                    setMax = modifier.setMax,
                    postFactor = modifier.postFactor,
                    statFactorMod = modifier.statFactorMod,
                    setMaxCurveOverride = modifier.setMaxCurveOverride,
                    setMaxCurveEvaluateStat = modifier.setMaxCurveEvaluateStat
                });
        }

        return copy;
    }
}
