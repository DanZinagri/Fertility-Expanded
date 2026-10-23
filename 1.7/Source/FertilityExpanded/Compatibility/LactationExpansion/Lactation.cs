using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace FertilityExpanded.Patches.Compatibility;

internal static class LactationExpansionCompat
{
    private static bool lookedForPawnIsLactating;
    private static MethodInfo pawnIsLactatingMethod;
    private static FieldInfo maximumNutritionStoredField;
    private static FieldInfo lactationPerDayField;
    private static FieldInfo lactationPerTickField;

    public static bool HasActiveLactation(Pawn pawn)
    {
        if (!ModDetection.LactationExpansionActive || pawn == null)
        {
            return false;
        }

        MethodInfo method = PawnIsLactatingMethod;
        if (method == null)
        {
            return false;
        }

        try
        {
            return method.Invoke(null, new object[] { pawn }) is bool result && result;
        }
        catch (Exception ex)
        {
            Log.WarningOnce("[Fertility Expanded] Lactation Expansion compatibility failed while checking active lactation: " + ex.Message, 17493201);
            return false;
        }
    }

    // LE always makes SEX_BreastMilk. Swap it for the gene or xenotype choice, keeping
    // the expressed nutrition; if the choice is too nutritious for one unit, keep LE's milk.
    public static void GenerateMilkPostfix(Pawn pawn, ref Thing __result)
    {
        if (!Features.LactationSystem || __result == null || __result.stackCount <= 0)
        {
            return;
        }

        ThingDef milkDef = XenotypeProfiles.ProducedMilkDef(pawn);
        if (milkDef == null || milkDef == __result.def)
        {
            return;
        }

        float nutrition = __result.stackCount * SelfMilking.MilkNutritionPerUnit(__result.def);
        int count = Mathf.FloorToInt(nutrition / SelfMilking.MilkNutritionPerUnit(milkDef));
        if (count <= 0)
        {
            return;
        }

        Thing milk = ThingMaker.MakeThing(milkDef);
        milk.stackCount = count;
        __result.Destroy();
        __result = milk;
    }

    // LE rebuilds capacity and production from its stats every 120 ticks; scale the
    // result by RJW breast size the same way FE's own milk production is scaled.
    public static void RecacheStatsPostfix(HediffComp __instance)
    {
        float factor = BreastLactationScaling.FactorFor(__instance.Pawn);
        if (factor == 1f)
        {
            return;
        }

        Type type = __instance.GetType();
        maximumNutritionStoredField ??= AccessTools.Field(type, "maximumNutritionStored");
        lactationPerDayField ??= AccessTools.Field(type, "lactationPerDay");
        lactationPerTickField ??= AccessTools.Field(type, "lactationPerTick");
        Scale(maximumNutritionStoredField, __instance, factor);
        Scale(lactationPerDayField, __instance, factor);
        Scale(lactationPerTickField, __instance, factor);
    }

    private static void Scale(FieldInfo field, object instance, float factor)
    {
        if (field?.GetValue(instance) is float value)
        {
            field.SetValue(instance, value * factor);
        }
    }

    private static MethodInfo PawnIsLactatingMethod
    {
        get
        {
            if (!lookedForPawnIsLactating)
            {
                lookedForPawnIsLactating = true;
                Type type = AccessTools.TypeByName("EuterpeMilkyTitfuck.LactationUtilities");
                pawnIsLactatingMethod = type == null ? null : AccessTools.Method(type, "PawnIsLactating", new[] { typeof(Pawn) });
                if (pawnIsLactatingMethod == null)
                {
                    Log.WarningOnce("[Fertility Expanded] Lactation Expansion is active, but EuterpeMilkyTitfuck.LactationUtilities.PawnIsLactating(Pawn) was not found. Vanilla lactation support will still work, but Lactation Expansion lactation states will not count for Fertility Expanded support checks.", 17493202);
                }
            }

            return pawnIsLactatingMethod;
        }
    }
}
