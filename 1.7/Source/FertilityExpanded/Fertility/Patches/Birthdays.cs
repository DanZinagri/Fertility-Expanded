using HarmonyLib;
using Verse;

namespace FertilityExpanded.Patches;

[HarmonyPatch(typeof(Pawn_AgeTracker), "BirthdayBiological")]
public static class BirthdaysPatch
{
    public static void Postfix(Pawn ___pawn, int birthdayAge)
    {
        Current.Game?.GetComponent<GameState>()?.NotifyBiologicalBirthday(___pawn, birthdayAge);
    }
}
