using System.Collections.Generic;
using FertilityExpanded.Patches.Compatibility;
using RimWorld;
using Verse;
using Verse.AI;

namespace FertilityExpanded;

public static class FeedingBeliefs
{
    private const string WetNurseRoleDefName = "FE_IdeoRole_WetNurse";
    private const string WetNursingDisapprovedDefName = "FE_WetNursing_Disapproved";
    private const string BreastfeedingRequiredDefName = "FE_InfantFeeding_BreastfeedingRequired";
    private const string BreastfeedingPreferredDefName = "FE_InfantFeeding_BreastfeedingPreferred";

    private static PreceptDef wetNursingDisapproved;
    private static PreceptDef breastfeedingRequired;
    private static PreceptDef breastfeedingPreferred;

    public static void HandleSuccessfulFeedingMemory(Pawn feeder, ThoughtDef thoughtDef, Pawn baby)
    {
        if (!ModsConfig.BiotechActive
            || feeder == null
            || baby == null
            || thoughtDef != ThoughtDefOf.BreastfedBaby)
        {
            return;
        }

        HandleBreastfedBaby(feeder, baby);
    }

    public static bool IsWetNurseRolePawn(Pawn pawn)
    {
        if (!ModsConfig.IdeologyActive
            || pawn?.Ideo == null)
        {
            return false;
        }

        List<Precept_Role> roles = pawn.Ideo.RolesListForReading;
        for (int i = 0; i < roles.Count; i++)
        {
            Precept_Role role = roles[i];
            if (role?.def?.defName == WetNurseRoleDefName && role.Active && role.IsAssigned(pawn))
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasActiveLactation(Pawn pawn)
    {
        return ModsConfig.BiotechActive
            && pawn != null
            && (pawn.health?.hediffSet?.HasHediff(HediffDefOf.Lactating) == true
                || LactationExpansionCompat.HasActiveLactation(pawn));
    }

    public static bool ShouldBlockArtificialFeeding(Pawn feeder, Pawn baby, Thing foodSource = null)
    {
        if (IsBreastMilkFood(foodSource))
        {
            return false;
        }

        if (!ModsConfig.BiotechActive || !ModsConfig.IdeologyActive || feeder == null || baby == null)
        {
            return false;
        }

        if (!RequiresBreastfeeding(feeder) && !RequiresBreastfeeding(baby))
        {
            return false;
        }

        return HasUsableLactatingCaregiverNow(baby);
    }

    public static bool IsBreastMilkFood(Thing food)
    {
        return food?.def != null
            && (food.def == FE_DefOf.FE_HumanMilk
                || ExternalMilkCompat.IsRecognizedBreastMilk(food.def));
    }

    public static Thing FindHumanMilkForBaby(Pawn feeder, Pawn baby)
    {
        if (!ModsConfig.BiotechActive || feeder == null || baby == null)
        {
            return null;
        }

        ThingOwner<Thing> inventory = feeder.inventory?.innerContainer;
        if (inventory != null)
        {
            for (int i = 0; i < inventory.Count; i++)
            {
                Thing milk = inventory[i];
                if (CanUseHumanMilkForBaby(feeder, baby, milk, requireReservation: false))
                {
                    return milk;
                }
            }
        }

        if (!feeder.Spawned)
        {
            return null;
        }

        return GenClosest.ClosestThingReachable(
            feeder.Position,
            feeder.Map,
            ThingRequest.ForGroup(ThingRequestGroup.FoodSource),
            PathEndMode.ClosestTouch,
            TraverseParms.For(feeder),
            9999f,
            (Thing milk) => CanUseHumanMilkForBaby(feeder, baby, milk, requireReservation: true));
    }

    private static void HandleBreastfedBaby(Pawn feeder, Pawn baby)
    {
        if (IsRecognizedParentOrFoster(feeder, baby))
        {
            return;
        }

        CareRecord care = Current.Game?.GetComponent<GameState>()?.FindPostpartumCare(baby);
        foreach (Pawn parent in ParentsWithMood(baby))
        {
            MemoryThoughtHandler memories = parent.needs.mood.thoughts.memories;
            if (PawnHasPrecept(parent, WetNursingDisapproved))
            {
                memories.RemoveMemoriesOfDefWhereOtherPawnIs(FE_DefOf.FE_BabyCaredFor, baby);
                if (CanGainFeedingDisapproval(memories))
                {
                    memories.TryGainMemory(FE_DefOf.FE_WetNursedAgainstBelief, baby);
                }
            }
            else
            {
                memories.RemoveMemoriesOfDefWhereOtherPawnIs(FE_DefOf.FE_WetNursedAgainstBelief, baby);
                if (care != null && !care.relievedParents.Contains(parent))
                {
                    memories.TryGainMemory(FE_DefOf.FE_BabyCaredFor, baby);
                    care.relievedParents.Add(parent);
                }
            }
        }
    }

    public static void HandleBottleFedBaby(Pawn baby, Thing foodSource)
    {
        if (!ModsConfig.BiotechActive || !ModsConfig.IdeologyActive || baby == null)
        {
            return;
        }

        if (foodSource == null || IsBreastMilkFood(foodSource))
        {
            return;
        }

        bool? lactatingCaregiverAvailable = null;
        foreach (Pawn parent in ParentsWithMood(baby))
        {
            ThoughtDef reaction = RequiresBreastfeeding(parent)
                ? FE_DefOf.FE_UsedFormulaAgainstBelief
                : PawnHasPrecept(parent, BreastfeedingPreferred)
                    ? FE_DefOf.FE_UsedFormulaDespitePreference
                    : null;
            MemoryThoughtHandler memories = parent.needs.mood.thoughts.memories;
            if (reaction == null || !CanGainFeedingDisapproval(memories))
            {
                continue;
            }

            lactatingCaregiverAvailable ??= HasAvailableLactatingCaregiver(baby);
            if (lactatingCaregiverAvailable != true)
            {
                return;
            }

            memories.TryGainMemory(reaction, baby);
        }
    }

    private static bool CanGainFeedingDisapproval(MemoryThoughtHandler memories)
    {
        List<Thought_Memory> existing = memories.Memories;
        for (int i = 0; i < existing.Count; i++)
        {
            ThoughtDef def = existing[i].def;
            if (def == FE_DefOf.FE_WetNursedAgainstBelief
                || def == FE_DefOf.FE_UsedFormulaAgainstBelief
                || def == FE_DefOf.FE_UsedFormulaDespitePreference
                || def == FE_DefOf.FE_FeedingDisapprovalCooldown)
            {
                return false;
            }
        }

        return true;
    }

    private static List<Pawn> ParentsWithMood(Pawn baby)
    {
        List<Pawn> parents = new List<Pawn>();
        if (baby.relations == null)
        {
            return parents;
        }

        foreach (DirectPawnRelation relation in baby.relations.DirectRelations)
        {
            Pawn parent = relation.otherPawn;
            if (FamilyRelations.IsParentLikeRelation(relation.def)
                && parent?.needs?.mood?.thoughts?.memories != null
                && !parent.Dead
                && !parent.Destroyed
                && !parents.Contains(parent))
            {
                parents.Add(parent);
            }
        }

        return parents;
    }

    private static bool CanUseHumanMilkForBaby(Pawn feeder, Pawn baby, Thing milk, bool requireReservation)
    {
        if (!IsBreastMilkFood(milk) || !milk.IngestibleNow || milk.IsForbidden(feeder) || baby.WillEat(milk, null, careIfNotAcceptableForTitle: false) == false)
        {
            return false;
        }

        return !requireReservation || feeder.CanReserve(milk);
    }

    private static bool HasAvailableLactatingCaregiver(Pawn baby)
    {
        if (baby == null || !baby.SpawnedOrAnyParentSpawned)
        {
            return false;
        }

        foreach (Pawn caregiver in ChildcareUtility.CanBreastfeedMothers(baby, includeAutoBreastfeedOff: true))
        {
            if (caregiver != null)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasUsableLactatingCaregiverNow(Pawn baby)
    {
        if (baby == null || !baby.SpawnedOrAnyParentSpawned)
        {
            return false;
        }

        foreach (Pawn caregiver in ChildcareUtility.CanBreastfeedMothers(baby))
        {
            if (caregiver != null && HasActiveLactation(caregiver) && ChildcareUtility.CanMomBreastfeedBabyNow(caregiver, baby, out _))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsRecognizedParentOrFoster(Pawn caregiver, Pawn baby)
    {
        return FamilyRelations.HasParentLikeRelation(baby, caregiver);
    }

    private static bool PawnHasPrecept(Pawn pawn, PreceptDef preceptDef)
    {
        return ModsConfig.IdeologyActive && preceptDef != null && pawn?.Ideo?.HasPrecept(preceptDef) == true;
    }

    private static bool RequiresBreastfeeding(Pawn pawn)
    {
        return PawnHasPrecept(pawn, BreastfeedingRequired);
    }

    private static PreceptDef WetNursingDisapproved => wetNursingDisapproved ??= DefDatabase<PreceptDef>.GetNamedSilentFail(WetNursingDisapprovedDefName);

    private static PreceptDef BreastfeedingRequired => breastfeedingRequired ??= DefDatabase<PreceptDef>.GetNamedSilentFail(BreastfeedingRequiredDefName);

    private static PreceptDef BreastfeedingPreferred => breastfeedingPreferred ??= DefDatabase<PreceptDef>.GetNamedSilentFail(BreastfeedingPreferredDefName);
}
