using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using Verse;

namespace FertilityExpanded.Patches;

[HarmonyPatch]
public static class BottleFeedingPatch
{
    public static bool Prepare()
    {
        return ModsConfig.BiotechActive && ModsConfig.IdeologyActive && TargetMethod() != null;
    }

    public static MethodBase TargetMethod()
    {
        MethodInfo target = null;
        foreach (MethodInfo method in AccessTools.GetDeclaredMethods(typeof(JobDriver_BottleFeedBaby)))
        {
            if (!method.Name.StartsWith("<FeedBabyFoodFromInventory>", StringComparison.Ordinal)
                || method.IsStatic
                || method.ReturnType != typeof(void)
                || !method.IsDefined(typeof(CompilerGeneratedAttribute), false))
            {
                continue;
            }

            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length != 1 || parameters[0].ParameterType != typeof(int))
            {
                continue;
            }

            if (target != null)
            {
                Log.Error("[Fertility Expanded] Found multiple bottle-feeding tick methods. Bottle-feeding belief memories were not patched.");
                return null;
            }

            target = method;
        }

        if (target == null)
        {
            Log.Error("[Fertility Expanded] Could not find the bottle-feeding tick method. Bottle-feeding belief memories were not patched.");
        }

        return target;
    }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> code = new List<CodeInstruction>(instructions);
        MethodInfo ingested = AccessTools.Method(typeof(Thing), nameof(Thing.Ingested), new[] { typeof(Pawn), typeof(float) });
        int matches = 0;
        foreach (CodeInstruction instruction in code)
        {
            if (instruction.Calls(ingested))
            {
                matches++;
            }
        }

        if (matches != 2)
        {
            Log.Error($"[Fertility Expanded] Expected two bottle-feeding ingestion calls, found {matches}. Bottle-feeding belief memories were not patched.");
            return code;
        }

        MethodInfo replacement = AccessTools.Method(typeof(BottleFeedingPatch), nameof(Ingest));
        foreach (CodeInstruction instruction in code)
        {
            if (instruction.Calls(ingested))
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = replacement;
            }
        }

        return code;
    }

    private static float Ingest(Thing food, Pawn baby, float nutritionWanted)
    {
        float nutrition = food.Ingested(baby, nutritionWanted);
        if (nutrition <= 0f
            || baby?.CurJobDef != JobDefOf.BabySuckle
            || FeedingBeliefs.IsBreastMilkFood(food))
        {
            return nutrition;
        }

        Pawn feeder = baby.CurJob.targetA.Pawn;
        if (feeder?.CurJobDef == JobDefOf.BottleFeedBaby
            && feeder.CurJob.targetA.Pawn == baby)
        {
            FeedingBeliefs.HandleBottleFedBaby(baby, food);
        }

        return nutrition;
    }
}
