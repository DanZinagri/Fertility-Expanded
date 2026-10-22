using System.Collections.Generic;
using FertilityExpanded.Patches.Compatibility;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace FertilityExpanded;

public static class PostpartumCare
{
    private const int CareCheckIntervalTicks = 15000;
    private const int RecentCareTicks = 30000;
    private const int CareNeglectStartTicks = 60000;
    internal const int CareCheckEndTicks = 180000;
    private const float BabyNearbyDistance = 25f;
    private const float SupportNearbyDistance = 8f;

    internal static void EvaluatePostpartumCare(CareRecord care, int currentTick)
    {
        Pawn pawn = care.birther;
        bool supportedMemoryEnabled = Features.SupportedPostpartumCareMemory;
        bool neglectedMemoryEnabled = Features.NeglectedPostpartumCareMemory;
        if ((!supportedMemoryEnabled && !neglectedMemoryEnabled)
            || !CanReceivePostpartumThoughts(pawn)
            || !pawn.Spawned)
        {
            care.goodReadings = 0;
            care.poorReadings = 0;
            return;
        }

        int careAgeTicks = currentTick - care.birthTick;
        if (careAgeTicks < CareCheckIntervalTicks || careAgeTicks > CareCheckEndTicks
            || (care.lastAssessmentTick >= 0 && currentTick - care.lastAssessmentTick < CareCheckIntervalTicks))
        {
            return;
        }

        if (care.lastAssessmentTick >= 0 && currentTick - care.lastAssessmentTick > CareCheckIntervalTicks * 2)
        {
            care.goodReadings = 0;
            care.poorReadings = 0;
        }

        int careSinceTick = currentTick - RecentCareTicks;
        bool hasSupport = care.lastCareTick >= 0 && care.lastCareTick > careSinceTick;
        int score = CareScore(pawn, care.babies);
        CarePolicy policy = PolicyFor(pawn);
        if (HasLivingBaby(care.babies))
        {
            if (care.lastLactatingCareTick >= 0 && care.lastLactatingCareTick > careSinceTick)
            {
                score += 2;
            }
            else if (care.lastWetNurseCareTick >= 0 && care.lastWetNurseCareTick > careSinceTick)
            {
                score++;
            }
        }

        care.lastAssessmentTick = currentTick;
        bool goodCare = supportedMemoryEnabled && score >= policy.SupportScore && hasSupport;
        bool poorCare = neglectedMemoryEnabled && score <= policy.NeglectScore;
        care.goodReadings = goodCare ? System.Math.Min(care.goodReadings + 1, 2) : 0;
        care.poorReadings = poorCare ? System.Math.Min(care.poorReadings + 1, 2) : 0;

        if (care.goodReadings >= 2)
        {
            ApplyCareMemory(pawn, FE_DefOf.FE_SupportedAfterBirth);
        }
        else if (care.poorReadings >= 2 && careAgeTicks >= policy.NeglectStartTicks)
        {
            ApplyCareMemory(pawn, policy.NeglectMemory);
        }
    }

    public static void RegisterRecovery(Pawn pawn)
    {
        if (!CanReceivePostpartumThoughts(pawn))
        {
            return;
        }

        GameState component = Current.Game?.GetComponent<GameState>();
        if (component == null || component.TryGetPostpartumCareWindow(pawn, out _, out _))
        {
            return;
        }

        Hediff recovery = IntimacyRecovery.GetActiveRecoveryHediff(pawn);
        Hediff exhaustion = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PostpartumExhaustion);
        if (exhaustion != null && (recovery == null || exhaustion.ageTicks < recovery.ageTicks))
        {
            recovery = exhaustion;
        }

        if (recovery != null && recovery.ageTicks >= 0 && recovery.ageTicks <= CareCheckEndTicks)
        {
            component.RegisterPostpartumCareWindow(pawn, Find.TickManager.TicksGame - recovery.ageTicks);
        }
    }

    public static void RegisterRecovery(Pawn pawn, HediffDef addedDef)
    {
        if (addedDef == HediffDefOf.PostpartumExhaustion || IntimacyRecovery.IsPostpartumRecovery(addedDef))
        {
            RegisterRecovery(pawn);
        }
    }

    internal static CareRecord CareRecipient(Pawn caregiver, Pawn recipient)
    {
        if (!ModsConfig.BiotechActive
            || (!Features.SupportedPostpartumCareMemory && !Features.NeglectedPostpartumCareMemory)
            || caregiver == null || caregiver.Dead || caregiver.Destroyed || caregiver == recipient
            || recipient == null || recipient.Dead || recipient.Destroyed || recipient.RaceProps?.Humanlike != true)
        {
            return null;
        }

        CareRecord care = Current.Game?.GetComponent<GameState>()?.FindPostpartumCare(recipient);
        return care?.birther != null && !care.birther.Dead && !care.birther.Destroyed
            && caregiver != care.birther && !caregiver.HostileTo(care.birther)
            ? care
            : null;
    }

    public static void NotifyCareCompleted(Pawn caregiver, Pawn recipient)
    {
        CareRecord care = CareRecipient(caregiver, recipient);
        if (care == null)
        {
            return;
        }

        int currentTick = Find.TickManager.TicksGame;
        care.lastCareTick = currentTick;
        if (FeedingBeliefs.HasActiveLactation(caregiver))
        {
            care.lastLactatingCareTick = currentTick;
        }
        else if (FeedingBeliefs.IsWetNurseRolePawn(caregiver))
        {
            care.lastWetNurseCareTick = currentTick;
        }
    }

    private static void ApplyCareMemory(Pawn pawn, ThoughtDef thought)
    {
        if (thought == null)
        {
            return;
        }

        MemoryThoughtHandler memories = pawn.needs.mood.thoughts.memories;
        if (FE_DefOf.FE_SupportedAfterBirth != null && thought != FE_DefOf.FE_SupportedAfterBirth)
        {
            memories.RemoveMemoriesOfDef(FE_DefOf.FE_SupportedAfterBirth);
        }
        if (FE_DefOf.FE_NeglectedAfterBirth != null && thought != FE_DefOf.FE_NeglectedAfterBirth)
        {
            memories.RemoveMemoriesOfDef(FE_DefOf.FE_NeglectedAfterBirth);
        }
        if (FE_DefOf.FE_SacredPostpartumNeglect != null && thought != FE_DefOf.FE_SacredPostpartumNeglect)
        {
            memories.RemoveMemoriesOfDef(FE_DefOf.FE_SacredPostpartumNeglect);
        }
        if (memories.GetFirstMemoryOfDef(thought) == null)
        {
            memories.TryGainMemory(thought);
        }
    }

    internal readonly struct CarePolicy
    {
        public readonly int SupportScore;
        public readonly int NeglectStartTicks;
        public readonly int NeglectScore;
        public readonly ThoughtDef NeglectMemory;

        public CarePolicy(int supportScore, int neglectStartTicks, int neglectScore, ThoughtDef neglectMemory)
        {
            SupportScore = supportScore;
            NeglectStartTicks = neglectStartTicks;
            NeglectScore = neglectScore;
            NeglectMemory = neglectMemory;
        }
    }

    internal static CarePolicy PolicyFor(Pawn mother)
    {
        if (FamilyPrecepts.HasPrecept(mother, "FE_PostpartumCare_SacredRest"))
        {
            return new CarePolicy(3, 30000, -2, FE_DefOf.FE_SacredPostpartumNeglect);
        }

        int neglectScore = FamilyPrecepts.HasPrecept(mother, "FE_PostpartumCare_Practical") ? -4 : -2;
        return new CarePolicy(4, CareNeglectStartTicks, neglectScore, FE_DefOf.FE_NeglectedAfterBirth);
    }

    public static void TryApplySingleParentStrain(Pawn birther, Pawn baby)
    {
        if (!Features.SingleParentStrain
            || !CanReceivePostpartumThoughts(birther)
            || baby == null
            || baby.Dead)
        {
            return;
        }

        if (!HasLovePartnerAtLocation(birther) && !HasOtherParentAtLocation(birther, baby))
        {
            birther.needs.mood.thoughts.memories.TryGainMemory(FE_DefOf.FE_SingleParentStrain);
        }
    }

    public static void TryApplyCoparentSupport(Pawn birther, Pawn baby)
    {
        if (!Features.CoparentSupport
            || !CanReceivePostpartumThoughts(birther)
            || baby == null
            || baby.Dead
            || FE_DefOf.FE_CoparentSupportedBirth == null)
        {
            return;
        }

        Pawn coparent = FindNearbyCoparent(birther, baby);
        if (coparent != null)
        {
            birther.needs.mood.thoughts.memories.TryGainMemory(FE_DefOf.FE_CoparentSupportedBirth, coparent);
        }
    }

    private static bool CanReceivePostpartumThoughts(Pawn pawn)
    {
        return ModsConfig.BiotechActive
            && pawn != null
            && !pawn.Dead
            && pawn.health != null
            && pawn.needs?.mood?.thoughts?.memories != null;
    }

    private static int CareScore(Pawn pawn, List<Pawn> babies)
    {
        int score = 0;

        if (pawn.CurrentBed() != null)
        {
            score += 2;
        }
        else if (!pawn.Downed)
        {
            score -= 1;
        }

        if (pawn.needs?.food != null)
        {
            if (pawn.needs.food.CurLevelPercentage >= 0.35f)
            {
                score++;
            }
            else if (pawn.needs.food.CurLevelPercentage < 0.2f)
            {
                score--;
            }
        }

        if (pawn.needs?.rest != null)
        {
            if (pawn.needs.rest.CurLevelPercentage >= 0.3f)
            {
                score++;
            }
            else if (pawn.needs.rest.CurLevelPercentage < 0.15f)
            {
                score--;
            }
        }

        if (pawn.health.hediffSet.PainTotal < 0.2f)
        {
            score++;
        }
        else if (pawn.health.hediffSet.PainTotal > 0.4f)
        {
            score--;
        }

        if (HasNearbyBaby(pawn, babies))
        {
            score++;
        }
        else
        {
            score--;
        }

        if (HasNearbyLovePartner(pawn) || HasNearbyOtherParent(pawn, babies))
        {
            score++;
        }

        return score;
    }

    private static bool IsLivingBaby(Pawn baby)
    {
        return baby != null && !baby.Dead && !baby.Destroyed && baby.RaceProps?.Humanlike == true;
    }

    private static bool HasLivingBaby(List<Pawn> babies)
    {
        if (babies == null)
        {
            return false;
        }

        for (int i = 0; i < babies.Count; i++)
        {
            if (IsLivingBaby(babies[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasNearbyBaby(Pawn pawn, List<Pawn> babies)
    {
        if (babies == null)
        {
            return false;
        }

        for (int i = 0; i < babies.Count; i++)
        {
            Pawn baby = babies[i];
            if (IsLivingBaby(baby) && BabyIsNearby(pawn, baby))
            {
                return true;
            }
        }

        return false;
    }

    private static bool BabyIsNearby(Pawn pawn, Pawn baby)
    {
        if (pawn.MapHeld == null || baby.MapHeld != pawn.MapHeld)
        {
            return false;
        }

        return pawn.PositionHeld.InHorDistOf(baby.PositionHeld, BabyNearbyDistance);
    }

    private static bool HasNearbyLovePartner(Pawn pawn)
    {
        Pawn partner = LovePartnerRelationUtility.ExistingLovePartner(pawn, allowDead: false);
        return IsNearbySupportPawn(pawn, partner);
    }

    private static bool HasNearbyOtherParent(Pawn birther, List<Pawn> babies)
    {
        if (babies == null)
        {
            return false;
        }

        for (int i = 0; i < babies.Count; i++)
        {
            Pawn baby = babies[i];
            if (IsLivingBaby(baby) && IsNearbySupportPawn(birther, FindOtherParentAtLocation(birther, baby)))
            {
                return true;
            }
        }

        return false;
    }

    private static Pawn FindNearbyCoparent(Pawn birther, Pawn baby)
    {
        Pawn partner = LovePartnerRelationUtility.ExistingLovePartner(birther, allowDead: false);
        if (IsNearbySupportPawn(birther, partner))
        {
            return partner;
        }

        Pawn otherParent = FindOtherParentAtLocation(birther, baby);
        if (IsNearbySupportPawn(birther, otherParent))
        {
            return otherParent;
        }

        return null;
    }

    private static bool IsNearbySupportPawn(Pawn pawn, Pawn supportPawn)
    {
        return supportPawn != null
            && !supportPawn.Dead
            && !supportPawn.Downed
            && !supportPawn.HostileTo(pawn)
            && SharesLocation(pawn, supportPawn)
            && (pawn.MapHeld == null || supportPawn.PositionHeld.InHorDistOf(pawn.PositionHeld, SupportNearbyDistance));
    }

    private static bool HasLovePartnerAtLocation(Pawn pawn)
    {
        Pawn partner = LovePartnerRelationUtility.ExistingLovePartner(pawn, allowDead: false);
        return partner != null && SharesLocation(pawn, partner);
    }

    private static bool HasOtherParentAtLocation(Pawn birther, Pawn baby)
    {
        return FindOtherParentAtLocation(birther, baby) != null;
    }

    private static Pawn FindOtherParentAtLocation(Pawn birther, Pawn baby)
    {
        if (baby.relations == null)
        {
            return null;
        }

        List<DirectPawnRelation> relations = baby.relations.DirectRelations;
        for (int i = 0; i < relations.Count; i++)
        {
            DirectPawnRelation relation = relations[i];
            Pawn otherParent = relation.otherPawn;
            if (relation.def == PawnRelationDefOf.Parent
                && otherParent != null
                && otherParent != birther
                && !otherParent.Dead
                && SharesLocation(birther, otherParent))
            {
                return otherParent;
            }
        }

        return null;
    }

    private static bool SharesLocation(Pawn pawn, Pawn other)
    {
        if (pawn.MapHeld != null)
        {
            return other.MapHeld == pawn.MapHeld;
        }

        Caravan caravan = pawn.GetCaravan();
        return caravan != null && other.GetCaravan() == caravan;
    }
}
