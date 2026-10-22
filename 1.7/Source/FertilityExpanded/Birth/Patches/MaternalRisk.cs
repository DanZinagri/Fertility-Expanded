using System.Collections.Generic;
using FertilityExpanded.Patches.Compatibility;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace FertilityExpanded.Patches;

[HarmonyPatch(typeof(PregnancyUtility), nameof(PregnancyUtility.ChanceMomDiesDuringBirth))]
public static class MaternalRiskPatch
{
    public static bool Prefix(ref float __result)
    {
        if (!BirthOutcomePatch.ApplyingExtraBirthOutcome)
        {
            return true;
        }

        __result = 0f;
        return false;
    }
}
