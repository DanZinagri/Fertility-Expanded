using System.Collections.Generic;
using FertilityExpanded.Patches.Compatibility;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace FertilityExpanded;

internal static class SelfMilking
{
    private const float DefaultMilkNutritionPerUnit = 0.05f;
    private const float LactationSeverityPerNutrition = 2f;

    public static float MilkNutritionPerUnit(Pawn pawn)
    {
        return MilkNutritionPerUnit(XenotypeProfiles.ProducedMilkDef(pawn));
    }

    public static float MilkNutritionPerUnit(ThingDef milkDef)
    {
        float nutrition = milkDef?.ingestible?.CachedNutrition ?? 0f;
        return nutrition > 0f ? nutrition : DefaultMilkNutritionPerUnit;
    }

    public static string SelfMilkDescription(Pawn pawn)
    {
        MilkStorage comp = LactationSelfMilkComp(pawn, out _);
        if (comp == null)
        {
            return "FE_SelfMilkDescription".Translate();
        }

        ThingDef milkDef = XenotypeProfiles.ProducedMilkDef(pawn);
        return "FE_SelfMilkStoredDescription".Translate(
                (milkDef?.label ?? "FE_MilkFallback".Translate().ToString()).Named("MILK"),
                comp.StoredMilkNutrition.ToStringByStyle(ToStringStyle.FloatMaxTwo).Named("STORED"),
                comp.MaxStoredMilkNutrition.ToStringByStyle(ToStringStyle.FloatMaxTwo).Named("CAPACITY"),
                comp.NursingReserve.ToStringByStyle(ToStringStyle.FloatMaxTwo).Named("RESERVE"));
    }

    public static bool AutomaticSelfMilkingEnabled(Pawn pawn)
    {
        return LactationSelfMilkComp(pawn, out _)?.AutomaticSelfMilking == true;
    }

    public static bool CanMilkSelf(Pawn pawn, out string reason)
    {
        bool canMilk = TryGetMilk(pawn, false, false, out _, out _, out string reasonKey);
        reason = reasonKey == null ? null : reasonKey.Translate().ToString();
        return canMilk;
    }

    public static bool CanAutomaticallyMilkSelf(Pawn pawn, bool requireFullStorage = false)
    {
        if (pawn?.IsColonistPlayerControlled != true || pawn.Drafted || pawn.Downed || pawn.InMentalState)
        {
            return false;
        }

        return TryGetMilk(pawn, true, requireFullStorage, out _, out _, out _);
    }

    private static bool TryGetMilk(
        Pawn pawn,
        bool requireAutomatic,
        bool requireFullStorage,
        out MilkStorage milkComp,
        out ThingDef milkDef,
        out string reasonKey)
    {
        milkComp = null;
        milkDef = null;
        reasonKey = null;

        if (!Features.LactationSystem)
        {
            reasonKey = "FE_LactationDisabled";
            return false;
        }

        if (!ModsConfig.BiotechActive)
        {
            reasonKey = "FE_BiotechRequired";
            return false;
        }

        if (ModDetection.ExternalMilkSystemActive)
        {
            reasonKey = "FE_ExternalMilkProduction";
            return false;
        }

        if (pawn == null || pawn.Dead || pawn.health?.hediffSet == null)
        {
            reasonKey = "FE_InvalidPawn";
            return false;
        }

        if (!pawn.Spawned || pawn.Map == null)
        {
            reasonKey = "FE_MustBeOnMap";
            return false;
        }

        milkComp = LactationSelfMilkComp(pawn, out _);
        if (milkComp == null)
        {
            reasonKey = "FE_NotLactating";
            return false;
        }

        if (requireAutomatic && !milkComp.AutomaticSelfMilking)
        {
            return false;
        }

        LactationProfile profile = XenotypeProfiles.ProfileFor(pawn);
        float capacity = milkComp.CapacityFor(profile);
        milkDef = profile.ProducedMilkDef;
        if (milkDef == null)
        {
            reasonKey = "FE_MilkMissing";
            return false;
        }

        if (milkComp.MilkUnitsFor(MilkNutritionPerUnit(milkDef)) <= 0)
        {
            reasonKey = "FE_NotEnoughMilk";
            return false;
        }

        return !requireFullStorage || milkComp.StoredMilkNutrition >= capacity;
    }

    public static bool TryMilkSelf(Pawn pawn, bool showMessages)
    {
        if (!TryGetMilk(pawn, false, false, out MilkStorage milkComp, out ThingDef milkDef, out string reasonKey))
        {
            if (showMessages && reasonKey != null)
            {
                Messages.Message(reasonKey.Translate(), pawn, MessageTypeDefOf.RejectInput, historical: false);
            }

            return false;
        }

        float nutritionPerUnit = MilkNutritionPerUnit(milkDef);
        int milkUnits = milkComp.MilkUnitsFor(nutritionPerUnit);
        if (milkUnits <= 0)
        {
            return false;
        }

        Thing milk = ThingMaker.MakeThing(milkDef);
        milk.stackCount = milkUnits;
        int placedUnits = 0;
        GenPlace.TryPlaceThing(milk, pawn.Position, pawn.Map, ThingPlaceMode.Near, out Thing placedMilk,
            (stack, count) => placedUnits += count);
        if (!milk.Spawned && !milk.Destroyed)
        {
            milk.Destroy();
        }
        if (placedUnits <= 0)
        {
            if (showMessages)
            {
                Messages.Message("FE_MilkPlacementFailed".Translate(), pawn, MessageTypeDefOf.RejectInput, false);
            }
            return false;
        }

        milkComp.ConsumeMilkUnits(nutritionPerUnit, placedUnits);
        float expressedNutrition = placedUnits * nutritionPerUnit;
        milkComp.parent.Severity = Mathf.Min(
            1f,
            milkComp.parent.Severity + expressedNutrition * LactationSeverityPerNutrition);

        if (showMessages && PawnUtility.ShouldSendNotificationAbout(pawn))
        {
            Messages.Message("FE_MilkProduced".Translate(pawn.Named("PAWN"), placedUnits.Named("COUNT"), milkDef.label.Named("MILK")), placedMilk ?? pawn, MessageTypeDefOf.PositiveEvent);
        }

        return true;
    }

    private static MilkStorage LactationSelfMilkComp(Pawn pawn, out Hediff lactating)
    {
        lactating = pawn?.health?.hediffSet?.GetFirstHediffOfDef(HediffDefOf.Lactating);
        return lactating?.TryGetComp<MilkStorage>();
    }
}
