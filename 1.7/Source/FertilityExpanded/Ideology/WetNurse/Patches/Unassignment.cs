using FertilityExpanded.Patches.Compatibility;
using HarmonyLib;
using RimWorld;
using Verse;

namespace FertilityExpanded;

[HarmonyPatch(typeof(Precept_Role), nameof(Precept_Role.Notify_PawnUnassigned))]
public static class WetNurseRemovalPatch
{
    public static void Postfix(Precept_Role __instance, Pawn oldPawn)
    {
        if (__instance.def.defName == WetNurseLactation.RoleDefName)
        {
            WetNurseLactation.SetRoleSource(oldPawn, false);
        }
    }
}
