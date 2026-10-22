using RimWorld;
using Verse;

namespace FertilityExpanded;

public static class CradleCommunity
{
    private const string CradleCommunityMemeDefName = "FE_CradleCommunity";

    private static MemeDef cradleCommunityMeme;

    public static bool HasCradleCommunity(Pawn pawn)
    {
        return pawn != null && HasCradleCommunity(pawn.Ideo);
    }

    public static bool HasCradleCommunity(Ideo ideo)
    {
        if (!ModsConfig.IdeologyActive || ideo == null)
        {
            return false;
        }

        MemeDef meme = CradleCommunityMeme;
        return meme != null && ideo.HasMeme(meme);
    }

    private static MemeDef CradleCommunityMeme => cradleCommunityMeme ??= DefDatabase<MemeDef>.GetNamedSilentFail(CradleCommunityMemeDefName);
}
