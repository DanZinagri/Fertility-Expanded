using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace FertilityExpanded.Patches.Compatibility;

// Scales FE milk storage and production with RimJobWorld breast size. Breast parts are
// the rjw.HediffDef_SexPart defs whose genitalFamily is Breasts (human, animal udders,
// slime, resin, generic). The tier comes from the part's current stage threshold,
// so translated stage labels don't matter.
internal static class BreastLactationScaling
{
    // Above zero: FE divides fill time by the production multiplier.
    public const float MinimumFactor = 0.05f;
    public const float MaximumFactor = 5f;
    public const float FactorStep = 0.05f;

    private const string SexPartDefTypeName = "rjw.HediffDef_SexPart";
    private const string GenitalFamilyFieldName = "genitalFamily";
    private const string BreastsFamilyName = "Breasts";

    public static readonly string[] TierLabels =
    {
        "Nipples", "Tiny", "Small", "Average", "Large", "Huge",
        "Enormous", "Massive", "Gargantuan", "Colossal", "Titanic", "Astronomical"
    };

    private static readonly float[] TierMinSeverities =
    {
        0.01f, 0.02f, 0.2f, 0.4f, 0.6f, 0.8f, 1f, 1.2f, 1.4f, 1.6f, 1.8f, 2f
    };

    // 15% per size step either side of Average.
    private static readonly float[] DefaultFactors =
    {
        0.55f, 0.7f, 0.85f, 1f, 1.15f, 1.3f, 1.45f, 1.6f, 1.75f, 1.9f, 2.05f, 2.2f
    };

    private static HashSet<HediffDef> breastDefs;

    public static int TierCount => TierLabels.Length;

    public static List<float> DefaultFactorList()
    {
        return new List<float>(DefaultFactors);
    }

    public static float FactorFor(Pawn pawn)
    {
        if (!ModDetection.RimJobWorldActive || !Features.RjwBreastLactationScaling)
        {
            return 1f;
        }

        List<Hediff> hediffs = pawn?.health?.hediffSet?.hediffs;
        if (hediffs == null || BreastDefs.Count == 0)
        {
            return 1f;
        }

        for (int i = 0; i < hediffs.Count; i++)
        {
            if (BreastDefs.Contains(hediffs[i].def))
            {
                // Use the stage RJW itself shows, so the tier always matches its size label.
                HediffStage stage = hediffs[i].CurStage;
                int tier = stage == null ? -1 : TierFor(stage.minSeverity);
                return tier < 0 ? 1f : Features.BreastLactationFactor(tier);
            }
        }

        return 1f;
    }

    private static HashSet<HediffDef> BreastDefs
    {
        get
        {
            if (breastDefs != null)
            {
                return breastDefs;
            }

            breastDefs = new HashSet<HediffDef>();
            List<HediffDef> hediffDefs = DefDatabase<HediffDef>.AllDefsListForReading;
            for (int i = 0; i < hediffDefs.Count; i++)
            {
                if (IsBreastPart(hediffDefs[i]))
                {
                    breastDefs.Add(hediffDefs[i]);
                }
            }

            if (breastDefs.Count == 0)
            {
                Log.WarningOnce("[Fertility Expanded] RimJobWorld is active, but no breast part definitions were found. Breast size will not affect milk production.", 173018290);
            }

            return breastDefs;
        }
    }

    private static int TierFor(float severity)
    {
        for (int i = TierMinSeverities.Length - 1; i >= 0; i--)
        {
            if (severity >= TierMinSeverities[i] - 0.0001f)
            {
                return i;
            }
        }

        return -1;
    }

    private static bool IsBreastPart(HediffDef hediffDef)
    {
        for (Type type = hediffDef.GetType(); type != null && type != typeof(HediffDef); type = type.BaseType)
        {
            if (type.FullName != SexPartDefTypeName)
            {
                continue;
            }

            FieldInfo field = AccessTools.Field(type, GenitalFamilyFieldName);
            return field?.GetValue(hediffDef)?.ToString() == BreastsFamilyName;
        }

        return false;
    }
}
