using FertilityExpanded.Patches.Compatibility;
using HarmonyLib;
using RimWorld;
using Verse;

namespace FertilityExpanded;

internal static class WetNurseLactation
{
    internal const string RoleDefName = "FE_IdeoRole_WetNurse";

    public static void SetRoleSource(Pawn pawn, bool assigned)
    {
        if (pawn?.health?.hediffSet == null || (assigned && pawn.Dead))
        {
            return;
        }

        Hediff source = pawn.health.hediffSet.GetFirstHediffOfDef(FE_DefOf.FE_WetNurseLactationSource);
        if (assigned && source == null)
        {
            pawn.health.AddHediff(FE_DefOf.FE_WetNurseLactationSource);
        }
        else if (!assigned && source != null)
        {
            pawn.health.RemoveHediff(source);
        }
    }

    public static void ReconcileAssignedNurses()
    {
        if (!ModsConfig.IdeologyActive || !ModsConfig.BiotechActive)
        {
            return;
        }

        foreach (Pawn pawn in PawnsFinder.AllMapsWorldAndTemporary_Alive)
        {
            SetRoleSource(pawn, pawn.Ideo?.GetRole(pawn)?.def?.defName == RoleDefName);
        }
    }

    internal static void MaintainLactation(Pawn pawn, bool allowMale)
    {
        if (!ModsConfig.BiotechActive
            || !Features.LactationSystem
            || ModDetection.LactationImplantMaintenanceBlocked
            || pawn?.health == null
            || pawn.Dead
            || (!allowMale && pawn.gender == Gender.Male)
            || pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Malnutrition)?.CurStageIndex >= 2)
        {
            return;
        }

        Hediff lactating = pawn.health.GetOrAddHediff(HediffDefOf.Lactating);
        if (lactating.Severity < 1f)
        {
            lactating.Severity = 1f;
        }
    }
}
