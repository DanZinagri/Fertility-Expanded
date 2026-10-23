using System.Collections.Generic;
using Verse;

namespace FertilityExpanded;

// Lets any GeneDef shape FE-managed lactation:
// <modExtensions>
//   <li Class="FertilityExpanded.LactationGeneExtension">
//     <milkDef>Milk</milkDef>
//     <storageCapacityFactor>1.5</storageCapacityFactor>
//     <productionRateFactor>2</productionRateFactor>
//   </li>
// </modExtensions>
// A gene's milk takes priority over the xenotype menu choice. Factors from every
// active gene multiply together and with the xenotype multipliers.
public class LactationGeneExtension : DefModExtension
{
    public ThingDef milkDef;
    public float storageCapacityFactor = 1f;
    public float productionRateFactor = 1f;

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors())
        {
            yield return error;
        }

        if (storageCapacityFactor <= 0f)
        {
            yield return "storageCapacityFactor must be greater than 0.";
        }

        if (productionRateFactor <= 0f)
        {
            yield return "productionRateFactor must be greater than 0.";
        }

        if (milkDef != null && milkDef.category != ThingCategory.Item)
        {
            yield return "milkDef " + milkDef.defName + " is not an item.";
        }
    }
}

internal static class LactationGenes
{
    private static List<GeneDef> genesWithExtension;

    public static IReadOnlyList<GeneDef> GenesWithExtension
    {
        get
        {
            if (genesWithExtension == null)
            {
                genesWithExtension = new List<GeneDef>();
                List<GeneDef> geneDefs = DefDatabase<GeneDef>.AllDefsListForReading;
                for (int i = 0; i < geneDefs.Count; i++)
                {
                    if (geneDefs[i].HasModExtension<LactationGeneExtension>())
                    {
                        genesWithExtension.Add(geneDefs[i]);
                    }
                }
            }

            return genesWithExtension;
        }
    }

    public static void Collect(
        Pawn pawn,
        out ThingDef milkDef,
        out float storageCapacityFactor,
        out float productionRateFactor)
    {
        milkDef = null;
        storageCapacityFactor = 1f;
        productionRateFactor = 1f;

        List<Gene> genes = pawn?.genes?.GenesListForReading;
        if (genes == null || GenesWithExtension.Count == 0)
        {
            return;
        }

        for (int i = 0; i < genes.Count; i++)
        {
            Gene gene = genes[i];
            LactationGeneExtension extension = gene.def.GetModExtension<LactationGeneExtension>();
            if (extension == null || !gene.Active)
            {
                continue;
            }

            milkDef ??= extension.milkDef;
            storageCapacityFactor *= extension.storageCapacityFactor;
            productionRateFactor *= extension.productionRateFactor;
        }
    }
}
