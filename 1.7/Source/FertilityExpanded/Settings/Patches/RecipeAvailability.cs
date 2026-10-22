using HarmonyLib;
using Verse;

namespace FertilityExpanded.Patches;

[HarmonyPatch(typeof(RecipeDef), nameof(RecipeDef.AvailableNow), MethodType.Getter)]
public static class RecipeAvailabilityPatch
{
    public static void Postfix(RecipeDef __instance, ref bool __result)
    {
        if (__result && !FeatureSettings.IsRecipeAvailable(__instance))
        {
            __result = false;
        }
    }
}
