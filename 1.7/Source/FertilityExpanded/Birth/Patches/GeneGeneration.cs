using HarmonyLib;
using Verse;

namespace FertilityExpanded.Patches;

[HarmonyPatch(typeof(PawnGenerator), "GenerateNewPawnInternal")]
public static class GeneGenerationPatch
{
    public static void Postfix(Pawn __result)
    {
        GeneCommonality.TryApply(__result);
    }
}
