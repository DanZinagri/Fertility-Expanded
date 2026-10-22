namespace FertilityExpanded.Patches.Compatibility;

internal static class RjwAfterSexPatch
{
    public static void Postfix(object props)
    {
        IntimateEvents.SyncRjwPawns(props);
    }
}
