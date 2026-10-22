using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace FertilityExpanded.Patches.Compatibility;

[HarmonyPatch(typeof(MemoryThoughtHandler))]
[HarmonyPatch(nameof(MemoryThoughtHandler.TryGainMemory), new Type[] { typeof(Thought_Memory), typeof(Pawn) })]
public static class LovinMemoriesPatch
{
    public static void Postfix(MemoryThoughtHandler __instance, Thought_Memory newThought, Pawn otherPawn)
    {
        IntimateEvents.NotifyMemoryGained(__instance?.pawn, newThought?.def, otherPawn);
    }
}
