using FertilityExpanded.Patches.Compatibility;
using HarmonyLib;
using RimWorld;
using Verse;

namespace FertilityExpanded;

[HarmonyPatch(typeof(Precept_Role), nameof(Precept_Role.Notify_PawnAssigned))]
public static class WetNurseAssignmentPatch
{
    public static void Postfix(Precept_Role __instance, Pawn newPawn)
    {
        if (__instance.def.defName == WetNurseLactation.RoleDefName)
        {
            WetNurseLactation.SetRoleSource(newPawn, true);
        }
    }
}
