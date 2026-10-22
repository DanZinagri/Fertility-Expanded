using Verse;

namespace FertilityExpanded.Patches.Compatibility;

internal static class RomanceCompat
{
    private const int MissingOrphanLoverFieldsWarningKey = 143764201;

    public static bool AdoptionChancePrefix()
    {
        return false;
    }

    public static void OrphanLoverAcceptedPostfix(object __instance)
    {
        bool foundBoy = ModDetection.TryReadPawnField(__instance, "boy", out Pawn boy);
        bool foundGirl = ModDetection.TryReadPawnField(__instance, "girl", out Pawn girl);
        if (!foundBoy || !foundGirl)
        {
            Log.WarningOnce(
                "[Fertility Expanded] Romance On The Rim's Orphan Lover fields do not match the supported API. Family-placement prompts were not added.",
                MissingOrphanLoverFieldsWarningKey);
            return;
        }

        TrySendFamilyPlacementPrompt(boy);
        if (girl != boy)
        {
            TrySendFamilyPlacementPrompt(girl);
        }
    }

    private static void TrySendFamilyPlacementPrompt(Pawn child)
    {
        if (!OrphanAdoption.TrySendPrompt(child, out _))
        {
            return;
        }

        Current.Game?.GetComponent<GameState>()?.NotifyOrphanAdoptionPromptSent(child);
    }
}
