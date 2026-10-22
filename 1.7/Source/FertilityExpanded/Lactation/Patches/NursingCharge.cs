using FertilityExpanded.Patches.Compatibility;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace FertilityExpanded.Patches;

[HarmonyPatch(typeof(HediffComp_Lactating), nameof(HediffComp_Lactating.TryCharge))]
public static class NursingChargePatch
{
    public static bool Prefix(HediffComp_Lactating __instance, ref float desiredChargeAmount)
    {
        MilkStorage storage = __instance.parent.TryGetComp<MilkStorage>();
        if (storage == null || __instance.Pawn?.needs?.food == null)
        {
            return true;
        }

        float oldRate = __instance.Props.ticksToFullCharge > 0
            ? __instance.Props.fullChargeAmount / __instance.Props.ticksToFullCharge
            : 0f;
        bool ownsSupply = Features.LactationSystem && !ModDetection.ExternalMilkSystemActive;
        if (ownsSupply)
        {
            storage.ApplyNursingSettings(XenotypeProfiles.ProfileFor(__instance.Pawn));
        }
        else
        {
            storage.RestoreNursingSettings();
        }
        if (oldRate > 0f && __instance.Props.ticksToFullCharge > 0)
        {
            float newRate = __instance.Props.fullChargeAmount / __instance.Props.ticksToFullCharge;
            desiredChargeAmount *= newRate / oldRate;
        }

        if (!ownsSupply)
        {
            return true;
        }

        float space = __instance.Props.fullChargeAmount - __instance.Charge;
        if (space <= 0f || desiredChargeAmount <= 0f)
        {
            return false;
        }

        float growth = PawnUtility.BodyResourceGrowthSpeed(__instance.Pawn);
        if (growth <= 0f)
        {
            return false;
        }

        desiredChargeAmount = Mathf.Min(desiredChargeAmount, space / growth);
        return true;
    }
}
