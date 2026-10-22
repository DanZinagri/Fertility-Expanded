using Verse;

namespace FertilityExpanded.Patches.Compatibility;

internal static class IntimacyPregnancyPatch
{
    public static void Postfix(Pawn initiator, Pawn partner)
    {
        IntimateEvents.NotifyIntimateMoment(initiator, partner);
    }
}
