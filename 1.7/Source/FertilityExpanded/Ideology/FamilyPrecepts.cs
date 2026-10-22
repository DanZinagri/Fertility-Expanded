using RimWorld;
using Verse;

namespace FertilityExpanded;

internal static class FamilyPrecepts
{
    public static bool HasPrecept(Pawn pawn, string defName)
    {
        return HasPrecept(pawn?.Ideo, defName);
    }

    public static bool HasPrecept(Ideo ideo, string defName)
    {
        if (!ModsConfig.IdeologyActive || ideo == null)
        {
            return false;
        }

        PreceptDef def = DefDatabase<PreceptDef>.GetNamedSilentFail(defName);
        return def != null && ideo.HasPrecept(def);
    }
}
