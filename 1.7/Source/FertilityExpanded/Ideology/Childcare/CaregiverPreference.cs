using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace FertilityExpanded;

internal static class CaregiverPreference
{
    private static readonly BottleFeedingEligibility bottleFeeding = new BottleFeedingEligibility();
    private static readonly WorkGiver_Breastfeed breastfeeding = new WorkGiver_Breastfeed();

    internal enum CarePolicy
    {
        None,
        Collective,
        Supported,
        FamilyLed
    }

    internal static CarePolicy PolicyFor(Pawn baby)
    {
        Ideo ideo = CareIdeoFor(baby);
        if (FamilyPrecepts.HasPrecept(ideo, "FE_CommunalParenting_Collective"))
        {
            return CarePolicy.Collective;
        }

        if (FamilyPrecepts.HasPrecept(ideo, "FE_CommunalParenting_FamilyLed"))
        {
            return CarePolicy.FamilyLed;
        }

        return FamilyPrecepts.HasPrecept(ideo, "FE_CommunalParenting_Supported") ? CarePolicy.Supported : CarePolicy.None;
    }

    private static Ideo CareIdeoFor(Pawn baby)
    {
        Ideo ideo = baby?.Ideo;
        if (ideo == null && baby?.relations != null)
        {
            foreach (DirectPawnRelation relation in baby.relations.DirectRelations)
            {
                if (FamilyRelations.IsParentLikeRelation(relation.def) && relation.otherPawn?.Ideo != null)
                {
                    ideo = relation.otherPawn.Ideo;
                    break;
                }
            }
        }

        return ideo;
    }

    internal static int Preference(Pawn caregiver, Pawn baby, CarePolicy policy)
    {
        if (policy != CarePolicy.Supported && policy != CarePolicy.FamilyLed)
        {
            return 0;
        }

        if (FamilyRelations.HasParentLikeRelation(baby, caregiver))
        {
            return policy == CarePolicy.FamilyLed ? 2 : 1;
        }

        return policy == CarePolicy.FamilyLed && IsExtendedFamilyCaregiver(caregiver, baby) ? 1 : 0;
    }

    private static bool IsExtendedFamilyCaregiver(Pawn caregiver, Pawn baby)
    {
        if (caregiver == null || caregiver == baby || baby?.relations == null)
        {
            return false;
        }

        bool adult = (caregiver.DevelopmentalStage & DevelopmentalStage.Adult) != 0;
        foreach (DirectPawnRelation relation in baby.relations.DirectRelations)
        {
            Pawn parent = relation.otherPawn;
            if (parent != null && FamilyRelations.IsParentLikeRelation(relation.def)
                && (FamilyRelations.HasParentLikeRelation(parent, caregiver)
                    || (adult && FamilyRelations.HasParentLikeRelation(caregiver, parent))))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool IsUrgentlyHungry(Pawn baby)
    {
        return baby?.needs?.food?.CurCategory >= HungerCategory.UrgentlyHungry;
    }

    internal static bool AllowsAutomaticFeeding(Pawn caregiver, Pawn baby)
    {
        return !ModsConfig.IdeologyActive || PolicyFor(baby) != CarePolicy.FamilyLed
            || IsUrgentlyHungry(baby) || Preference(caregiver, baby, CarePolicy.FamilyLed) > 0;
    }

    internal static bool ShouldDeferAutomaticFeeding(Pawn caregiver, Pawn baby, bool breastfeed)
    {
        return ModsConfig.IdeologyActive && PolicyFor(baby) == CarePolicy.FamilyLed
            && ShouldDefer(caregiver, baby, breastfeed ? breastfeeding : bottleFeeding, false);
    }

    public static IEnumerable<Pawn> OrderBreastfeeders(IEnumerable<Pawn> candidates, Pawn baby, CarePolicy policy, bool includeAutoBreastfeedOff)
    {
        if (policy == CarePolicy.None || policy == CarePolicy.Collective)
        {
            return candidates;
        }

        List<Pawn> ordered = new List<Pawn>();
        foreach (Pawn candidate in candidates)
        {
            if (policy != CarePolicy.FamilyLed || includeAutoBreastfeedOff || IsUrgentlyHungry(baby)
                || Preference(candidate, baby, policy) > 0)
            {
                ordered.Add(candidate);
            }
        }
        for (int i = 1; i < ordered.Count; i++)
        {
            Pawn candidate = ordered[i];
            int j = i;
            while (j > 0 && ComesBefore(candidate, ordered[j - 1], baby, policy))
            {
                ordered[j] = ordered[j - 1];
                j--;
            }

            ordered[j] = candidate;
        }

        return ordered;
    }

    private static bool ComesBefore(Pawn first, Pawn second, Pawn baby, CarePolicy policy)
    {
        AutofeedMode firstMode = baby.mindState.AutofeedSetting(first);
        AutofeedMode secondMode = baby.mindState.AutofeedSetting(second);
        if (firstMode != secondMode)
        {
            return FeedingPriority(firstMode) < FeedingPriority(secondMode);
        }

        return Preference(first, baby, policy) > Preference(second, baby, policy);
    }

    private static int FeedingPriority(AutofeedMode mode)
    {
        return mode == AutofeedMode.Urgent ? 0 : mode == AutofeedMode.Childcare ? 1 : 2;
    }

    public static bool ShouldDefer(Pawn caregiver, Pawn baby, WorkGiver_Scanner workGiver, bool forced)
    {
        if (forced || !ModsConfig.IdeologyActive || baby?.Spawned != true)
        {
            return false;
        }

        CarePolicy policy = PolicyFor(baby);
        int preference = Preference(caregiver, baby, policy);
        if (policy == CarePolicy.None || policy == CarePolicy.Collective
            || preference == (policy == CarePolicy.FamilyLed ? 2 : 1))
        {
            return false;
        }

        if (policy == CarePolicy.FamilyLed)
        {
            bool emergencyFeeding = IsUrgentlyHungry(baby)
                && (workGiver is WorkGiver_Breastfeed || workGiver is WorkGiver_BottleFeedBaby);
            if (preference == 0)
            {
                return !emergencyFeeding;
            }

            if (emergencyFeeding)
            {
                return false;
            }
        }
        else if (baby.needs?.food?.CurCategory >= HungerCategory.UrgentlyHungry)
        {
            return false;
        }

        foreach (Pawn candidate in baby.Map.mapPawns.FreeColonistsSpawned)
        {
            if (candidate == caregiver || Preference(candidate, baby, policy) <= preference
                || candidate.Drafted || candidate.Downed || candidate.InMentalState || !candidate.Awake()
                || (candidate.workSettings?.WorkIsActive(WorkTypeDefOf.Childcare) != true
                    && !(policy == CarePolicy.FamilyLed && !(workGiver is WorkGiver_PlayWithBaby)
                        && baby.mindState.AutofeedSetting(candidate) == AutofeedMode.Urgent))
                || (candidate.CurJob != null && candidate.CurJob.def != JobDefOf.Wait_Wander)
                || candidate.GetLord() != null
                || candidate.needs?.food?.CurCategory >= HungerCategory.UrgentlyHungry
                || candidate.needs?.rest?.CurLevelPercentage < 0.2f)
            {
                continue;
            }

            if (!(workGiver is WorkGiver_PlayWithBaby)
                && baby.mindState.AutofeedSetting(candidate) == AutofeedMode.Never)
            {
                continue;
            }

            if (workGiver is WorkGiver_Breastfeed
                && (baby.mindState.AutofeedSetting(candidate) == AutofeedMode.Childcare
                    || (policy == CarePolicy.FamilyLed && baby.mindState.AutofeedSetting(candidate) == AutofeedMode.Urgent))
                && ChildcareUtility.CanMomAutoBreastfeedBabyNow(candidate, baby, false, out _))
            {
                return true;
            }

            if (workGiver is WorkGiver_BottleFeedBaby
                && bottleFeeding.CanFeed(candidate, baby))
            {
                Thing food = ChildcareUtility.FindBabyFoodForBaby(candidate, baby);
                if (food != null && (!FeedingBeliefs.ShouldBlockArtificialFeeding(candidate, baby, food)
                    || FeedingBeliefs.FindHumanMilkForBaby(candidate, baby) != null))
                {
                    return true;
                }
            }

            if (workGiver is WorkGiver_PlayWithBaby && workGiver.HasJobOnThing(candidate, baby))
            {
                return true;
            }
        }

        return false;
    }

    public static void NotifyMeaningfulCare(Pawn caregiver, Pawn baby)
    {
        if (caregiver == null || baby == null
            || FamilyRelations.HasParentLikeRelation(baby, caregiver)
            || !FamilyPrecepts.HasPrecept(caregiver, "FE_CommunalParenting_Collective")
            || PolicyFor(baby) != CarePolicy.Collective || caregiver.Ideo != CareIdeoFor(baby))
        {
            return;
        }

        caregiver.needs?.mood?.thoughts?.memories?.TryGainMemory(FE_DefOf.FE_SharedChildcare, baby);
    }

    private sealed class BottleFeedingEligibility : WorkGiver_BottleFeedBaby
    {
        public bool CanFeed(Pawn caregiver, Pawn baby)
        {
            return CanCreateManualFeedingJob(caregiver, baby, false);
        }
    }
}
