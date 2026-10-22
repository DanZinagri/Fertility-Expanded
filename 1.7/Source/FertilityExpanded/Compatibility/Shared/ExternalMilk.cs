using Verse;

namespace FertilityExpanded.Patches.Compatibility;

internal static class ExternalMilkCompat
{
    private const string LactationExpansionMilkDefName = "SEX_BreastMilk";
    private const string EqualMilkingMilkDefName = "EM_HumanMilk";

    private static bool initialized;
    private static ThingDef lactationExpansionMilk;
    private static ThingDef equalMilkingMilk;

    public static void Initialize()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        if (ModDetection.LactationExpansionActive)
        {
            lactationExpansionMilk = DefDatabase<ThingDef>.GetNamedSilentFail(LactationExpansionMilkDefName);
        }

        if (ModDetection.EqualMilkingActive || ModDetection.HumanCattleActive)
        {
            equalMilkingMilk = DefDatabase<ThingDef>.GetNamedSilentFail(EqualMilkingMilkDefName);
        }
    }

    public static bool IsRecognizedBreastMilk(ThingDef thingDef)
    {
        Initialize();
        return thingDef != null
            && (thingDef == lactationExpansionMilk || thingDef == equalMilkingMilk);
    }

    public static ThingDef DominantMilkDef
    {
        get
        {
            Initialize();
            if ((ModDetection.EqualMilkingActive || ModDetection.HumanCattleActive)
                && equalMilkingMilk != null)
            {
                return equalMilkingMilk;
            }

            if (ModDetection.LactationExpansionActive && lactationExpansionMilk != null)
            {
                return lactationExpansionMilk;
            }

            return FE_DefOf.FE_HumanMilk;
        }
    }
}
