using System;
using System.Collections.Generic;
using FertilityExpanded.Patches.Compatibility;
using RimWorld;
using UnityEngine;
using Verse;

namespace FertilityExpanded;

public sealed class XenotypeSettings : IExposable
{
    public string xenotypeDefName;
    public float storageCapacityMultiplier = 1f;
    public float productionRateMultiplier = 1f;
    public string producedMilkDefName;

    public XenotypeSettings()
    {
    }

    public XenotypeSettings(string xenotypeDefName)
    {
        this.xenotypeDefName = xenotypeDefName;
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref xenotypeDefName, "xenotypeDefName");
        Scribe_Values.Look(ref storageCapacityMultiplier, "storageCapacityMultiplier", 1f);
        Scribe_Values.Look(ref productionRateMultiplier, "productionRateMultiplier", 1f);
        Scribe_Values.Look(ref producedMilkDefName, "producedMilkDefName");
    }

    public void Validate()
    {
        if (float.IsNaN(storageCapacityMultiplier) || float.IsInfinity(storageCapacityMultiplier))
        {
            storageCapacityMultiplier = 1f;
        }

        if (float.IsNaN(productionRateMultiplier) || float.IsInfinity(productionRateMultiplier))
        {
            productionRateMultiplier = 1f;
        }

        storageCapacityMultiplier = Mathf.Clamp(
            storageCapacityMultiplier,
            XenotypeProfiles.MinimumMultiplier,
            XenotypeProfiles.MaximumMultiplier);
        productionRateMultiplier = Mathf.Clamp(
            productionRateMultiplier,
            XenotypeProfiles.MinimumMultiplier,
            XenotypeProfiles.MaximumMultiplier);
    }
}

internal readonly struct LactationProfile
{
    public LactationProfile(
        ThingDef producedMilkDef,
        float storageCapacityMultiplier,
        float productionRateMultiplier)
    {
        ProducedMilkDef = producedMilkDef;
        StorageCapacityMultiplier = storageCapacityMultiplier;
        ProductionRateMultiplier = productionRateMultiplier;
    }

    public ThingDef ProducedMilkDef { get; }
    public float StorageCapacityMultiplier { get; }
    public float ProductionRateMultiplier { get; }
}
