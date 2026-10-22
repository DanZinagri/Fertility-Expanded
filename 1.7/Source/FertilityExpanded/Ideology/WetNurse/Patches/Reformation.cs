using FertilityExpanded.Patches.Compatibility;
using HarmonyLib;
using RimWorld;
using Verse;

namespace FertilityExpanded;

[HarmonyPatch(typeof(Precept_Role), nameof(Precept_Role.Notify_RemovedByReforming))]
public static class WetNurseReformPatch
{
    public static void Postfix(Precept_Role __instance)
    {
        if (__instance.def.defName == WetNurseLactation.RoleDefName)
        {
            foreach (Pawn pawn in __instance.ChosenPawns())
            {
                WetNurseLactation.SetRoleSource(pawn, false);
            }
        }
    }
}
