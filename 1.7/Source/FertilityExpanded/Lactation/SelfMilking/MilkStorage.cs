using System.Collections.Generic;
using FertilityExpanded.Patches.Compatibility;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace FertilityExpanded;

public class MilkStorage : HediffComp
{
    private bool automaticSelfMilking;
    private HediffComp_Lactating nursing;
    private HediffCompProperties_Chargeable originalNursingProperties;
    private HediffCompProperties_Lactating adjustedNursingProperties;

    internal HediffComp_Lactating Nursing => nursing ??= parent.TryGetComp<HediffComp_Lactating>();

    public float StoredMilkNutrition => Nursing?.Charge ?? 0f;

    public bool AutomaticSelfMilking
    {
        get => automaticSelfMilking;
        set => automaticSelfMilking = value;
    }

    public float MaxStoredMilkNutrition => Nursing?.Props.fullChargeAmount ?? 0f;

    public float NursingReserve => Nursing == null
        ? 0f
        : Mathf.Max(MaxStoredMilkNutrition * 0.5f, Nursing.Props.minChargeToActivate);

    internal float CapacityFor(LactationProfile profile)
    {
        ApplyNursingSettings(profile);
        return MaxStoredMilkNutrition;
    }

    public int AvailableMilkUnits
    {
        get
        {
            LactationProfile profile = XenotypeProfiles.ProfileFor(Pawn);
            float nutritionPerUnit = SelfMilking.MilkNutritionPerUnit(profile.ProducedMilkDef);
            return MilkUnitsFor(nutritionPerUnit);
        }
    }

    internal int MilkUnitsFor(float nutritionPerUnit)
    {
        return nutritionPerUnit > 0f
            ? Mathf.FloorToInt(Mathf.Max(0f, StoredMilkNutrition - NursingReserve) / nutritionPerUnit)
            : 0;
    }

    internal void ApplyNursingSettings(LactationProfile profile)
    {
        HediffComp_Lactating supply = Nursing;
        if (supply == null)
        {
            return;
        }

        if (originalNursingProperties == null
            || (supply.props != originalNursingProperties && supply.props != adjustedNursingProperties))
        {
            originalNursingProperties = supply.Props;
            adjustedNursingProperties = null;
        }

        if (originalNursingProperties.GetType() != typeof(HediffCompProperties_Lactating)
            || originalNursingProperties.fullChargeAmount <= 0f
            || originalNursingProperties.ticksToFullCharge <= 0)
        {
            Log.WarningOnce("[Fertility Expanded] Nonstandard nursing properties retain their existing milk capacity and production rate.", 173018271);
            return;
        }

        if (profile.StorageCapacityMultiplier == 1f && profile.ProductionRateMultiplier == 1f)
        {
            supply.props = originalNursingProperties;
        }
        else
        {
            adjustedNursingProperties ??= new HediffCompProperties_Lactating
            {
                compClass = originalNursingProperties.compClass,
                initialCharge = originalNursingProperties.initialCharge,
                minChargeToActivate = originalNursingProperties.minChargeToActivate,
                labelInBrackets = originalNursingProperties.labelInBrackets
            };
            float capacity = Mathf.Max(originalNursingProperties.minChargeToActivate,
                originalNursingProperties.fullChargeAmount * profile.StorageCapacityMultiplier);
            adjustedNursingProperties.fullChargeAmount = capacity;
            adjustedNursingProperties.ticksToFullCharge = Mathf.Max(1, Mathf.RoundToInt(
                originalNursingProperties.ticksToFullCharge
                * (capacity / originalNursingProperties.fullChargeAmount)
                / profile.ProductionRateMultiplier));
            supply.props = adjustedNursingProperties;
        }

        if (supply.Charge > supply.Props.fullChargeAmount)
        {
            supply.GreedyConsume(supply.Charge - supply.Props.fullChargeAmount);
        }
    }

    internal void RestoreNursingSettings()
    {
        if (nursing != null && nursing.props == adjustedNursingProperties)
        {
            nursing.props = originalNursingProperties;
            if (nursing.Charge > originalNursingProperties.fullChargeAmount)
            {
                nursing.GreedyConsume(nursing.Charge - originalNursingProperties.fullChargeAmount);
            }
        }
    }

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Values.Look(ref automaticSelfMilking, "automaticSelfMilking", false);
    }

    public int ConsumeAvailableMilkUnits(float nutritionPerUnit)
    {
        int units = MilkUnitsFor(nutritionPerUnit);
        return ConsumeMilkUnits(nutritionPerUnit, units);
    }

    internal int ConsumeMilkUnits(float nutritionPerUnit, int units)
    {
        units = Mathf.Min(units, MilkUnitsFor(nutritionPerUnit));
        if (units <= 0)
        {
            return 0;
        }

        Nursing.GreedyConsume(units * nutritionPerUnit);
        return units;
    }

    public override IEnumerable<Gizmo> CompGetGizmos()
    {
        IEnumerable<Gizmo> baseGizmos = base.CompGetGizmos();
        if (baseGizmos != null)
        {
            foreach (Gizmo gizmo in baseGizmos)
            {
                yield return gizmo;
            }
        }

        if (!Features.LactationSystem || Pawn?.IsColonistPlayerControlled != true)
        {
            yield break;
        }

        if (ModDetection.ExternalMilkSystemActive)
        {
            yield break;
        }

        Command_Toggle command = new Command_Toggle
        {
            defaultLabel = "FE_AutomaticSelfMilking".Translate(),
            defaultDesc = SelfMilking.SelfMilkDescription(Pawn),
            icon = XenotypeProfiles.ProducedMilkDef(Pawn)?.uiIcon ?? BaseContent.BadTex,
            isActive = () => automaticSelfMilking,
            toggleAction = () => automaticSelfMilking = !automaticSelfMilking
        };

        if (XenotypeProfiles.ProducedMilkDef(Pawn) == null)
        {
            command.Disable("FE_MilkMissing".Translate());
        }

        yield return command;
    }
}

public class MilkStorageProperties : HediffCompProperties
{
    public MilkStorageProperties()
    {
        compClass = typeof(MilkStorage);
    }
}
