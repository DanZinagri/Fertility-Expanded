using System.Text;
using FertilityExpanded.Patches.Compatibility;
using HarmonyLib;
using RimWorld;
using Verse;

namespace FertilityExpanded.Patches;

[HarmonyPatch(typeof(StatPart_FertilityByHediffs), nameof(StatPart_FertilityByHediffs.Factor))]
public static class InducedFertilityPatch
{
    public static void Postfix(Pawn pawn, StringBuilder explanation, ref float __result)
    {
        if (!ModsConfig.BiotechActive
            || !Features.LactationSystem
            || ModDetection.LactationImplantMaintenanceBlocked
            || pawn?.health?.hediffSet == null)
        {
            return;
        }

        bool induced = false;
        float lactationFactor = 1f;
        var hediffs = pawn.health.hediffSet.hediffs;
        for (int i = 0; i < hediffs.Count; i++)
        {
            Hediff hediff = hediffs[i];
            if (hediff.def == FE_DefOf.FE_LactationImplant
                || (ModsConfig.IdeologyActive && hediff.def == FE_DefOf.FE_WetNurseLactationSource))
            {
                induced = true;
            }
            else if (hediff.def == HediffDefOf.Lactating)
            {
                lactationFactor *= hediff.CurStage?.fertilityFactor ?? 1f;
            }
        }

        induced = induced || WetNurseGene.IsActiveCarrier(pawn);

        if (!induced || lactationFactor <= 0f || lactationFactor >= 1f)
        {
            return;
        }

        __result /= lactationFactor;
        explanation?.AppendLine("FE_InducedLactationFertility".Translate(
            (1f / lactationFactor).ToStringPercent().Named("FACTOR")));
    }
}
