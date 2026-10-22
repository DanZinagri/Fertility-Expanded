using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FertilityExpanded.Patches.Compatibility;

internal static class MinotaurCompat
{
    private const string LactationAbilityDefName = "RBM_Lactation";
    private const string PeriodicLactationGeneDefName = "RBM_PeriodicLactation";
    private const float MaintainedLactationSeverity = 1f;

    private static bool initialized;
    private static AbilityDef lactationAbility;
    private static GeneDef periodicLactationGene;

    public static void Initialize()
    {
        if (initialized || !ModDetection.RoosMinotaurActive)
        {
            return;
        }

        initialized = true;
        lactationAbility = DefDatabase<AbilityDef>.GetNamedSilentFail(LactationAbilityDefName);
        periodicLactationGene = DefDatabase<GeneDef>.GetNamedSilentFail(PeriodicLactationGeneDefName);

        if (lactationAbility == null)
        {
            Log.Warning("[Fertility Expanded] Roo's Minotaur is active, but RBM_Lactation was not found. Its original lactation ability could not be disabled.");
        }

        if (periodicLactationGene == null)
        {
            Log.Warning("[Fertility Expanded] Roo's Minotaur is active, but RBM_PeriodicLactation was not found. Roo gene carriers cannot be connected to the active lactation system.");
        }
    }

    public static void SynchronizeAllPawns()
    {
        if (!CanMaintainGeneLactation())
        {
            return;
        }

        List<Pawn> pawns = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive;
        for (int i = 0; i < pawns.Count; i++)
        {
            EnsureLactation(pawns[i]);
        }
    }

    public static void SynchronizeTravellingPawns()
    {
        if (!CanMaintainGeneLactation())
        {
            return;
        }

        List<Pawn> pawns = PawnsFinder.AllCaravansAndTravellingTransporters_Alive;
        for (int i = 0; i < pawns.Count; i++)
        {
            EnsureLactation(pawns[i]);
        }
    }

    public static void EnsureLactation(Pawn pawn)
    {
        if (pawn?.Dead != false
            || pawn.gender != Gender.Female
            || pawn.RaceProps?.Humanlike != true
            || pawn.health?.hediffSet == null
            || pawn.genes?.HasActiveGene(periodicLactationGene) != true
            || pawn.health.hediffSet.HasHediff(HediffDefOf.Malnutrition))
        {
            return;
        }

        Hediff lactating = pawn.health.GetOrAddHediff(HediffDefOf.Lactating);
        if (lactating.Severity < MaintainedLactationSeverity)
        {
            lactating.Severity = MaintainedLactationSeverity;
        }
    }

    public static void GeneratedPawnPostfix(Pawn __result)
    {
        if (CanMaintainGeneLactation())
        {
            EnsureLactation(__result);
        }
    }

    public static void CanCastPostfix(CompAbilityEffect __instance, ref bool __result)
    {
        if (IsRooLactationAbility(__instance))
        {
            __result = false;
        }
    }

    public static void ShouldHideGizmoPostfix(CompAbilityEffect __instance, ref bool __result)
    {
        if (IsRooLactationAbility(__instance))
        {
            __result = true;
        }
    }

    public static bool ApplyPrefix(CompAbilityEffect __instance)
    {
        return !IsRooLactationAbility(__instance);
    }

    public static bool CanMaintainGeneLactation()
    {
        Initialize();
        return ModDetection.RoosMinotaurActive
            && ModsConfig.BiotechActive
            && periodicLactationGene != null
            && !ModDetection.LactationImplantMaintenanceBlocked
            && (Features.LactationSystem || ModDetection.LactationExpansionActive);
    }

    private static bool IsRooLactationAbility(CompAbilityEffect abilityComp)
    {
        Initialize();
        return abilityComp?.parent?.def != null
            && (abilityComp.parent.def == lactationAbility
                || abilityComp.parent.def.defName == LactationAbilityDefName);
    }
}
