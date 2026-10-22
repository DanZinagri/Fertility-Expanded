using HarmonyLib;
using RimWorld;
using Verse;

namespace FertilityExpanded.Patches;

[HarmonyPatch(typeof(Designator_Adopt), nameof(Designator_Adopt.DesignateThing))]
public static class VanillaAdoptionPatch
{
    private static bool completingAdoption;

    public static bool Prefix(Designator_Adopt __instance, Thing t)
    {
        if (completingAdoption
            || t is not Pawn pawn
            || !ParentAssignment.CanChooseAdoptiveParentForVanillaAdoption(pawn))
        {
            return true;
        }

        ParentAssignment.OpenVanillaAdoptionParentPicker(
            pawn,
            delegate(Pawn parent)
            {
                completingAdoption = true;
                try
                {
                    __instance.DesignateThing(pawn);
                }
                finally
                {
                    completingAdoption = false;
                }

                if (parent != null && pawn.Faction == Faction.OfPlayer
                    && !ParentAssignment.TryAssignParent(pawn, parent, ParentKind.Adoptive))
                {
                    ParentAssignment.SendAssignmentLetter(pawn, ParentKind.Adoptive);
                }
            });
        return false;
    }
}
