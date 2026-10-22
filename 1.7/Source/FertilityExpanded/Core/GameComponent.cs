using System.Collections.Generic;
using FertilityExpanded.Patches.Compatibility;
using RimWorld;
using Verse;

namespace FertilityExpanded;

public class GameState : GameComponent
{
    private List<FosterPlacementRecord> fosterPlacements = new List<FosterPlacementRecord>();
    private List<OrphanAdoptionRecord> orphanAdoptions = new List<OrphanAdoptionRecord>();
    private Dictionary<int, int> lastCycleOnsetRollAges = new Dictionary<int, int>();
    private Dictionary<int, int> cycleOnsetAges = new Dictionary<int, int>();
    private Dictionary<int, int> lastMenopauseRollAges = new Dictionary<int, int>();
    private List<Pawn> deferredMenopauseChecks = new List<Pawn>();
    private Dictionary<int, CareRecord> postpartumCare = new Dictionary<int, CareRecord>();

    public GameState(Game game)
    {
    }

    public override void FinalizeInit()
    {
        base.FinalizeInit();
        PostpartumExhaustion.MigrateLegacyInstances();
        FeatureSettings.Apply();
        RjwDrugs.SynchronizeLoadedDrugPolicies();
        MinotaurCompat.SynchronizeAllPawns();
        WetNurseLactation.ReconcileAssignedNurses();
        ReconcilePostpartumCare();
        CheckFosterPlacementReviews();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref fosterPlacements, "fosterPlacements", LookMode.Deep);
        Scribe_Collections.Look(ref orphanAdoptions, "orphanAdoptions", LookMode.Deep);
        Scribe_Collections.Look(ref lastCycleOnsetRollAges, "lastCycleOnsetRollAges", LookMode.Value, LookMode.Value);
        Scribe_Collections.Look(ref cycleOnsetAges, "cycleOnsetAges", LookMode.Value, LookMode.Value);
        Scribe_Collections.Look(ref lastMenopauseRollAges, "lastMenopauseRollAges", LookMode.Value, LookMode.Value);
        Scribe_Collections.Look(ref deferredMenopauseChecks, "deferredMenopauseChecks", LookMode.Reference);
        Scribe_Collections.Look(ref postpartumCare, "postpartumCare", LookMode.Value, LookMode.Deep);
        fosterPlacements ??= new List<FosterPlacementRecord>();
        orphanAdoptions ??= new List<OrphanAdoptionRecord>();
        lastCycleOnsetRollAges ??= new Dictionary<int, int>();
        cycleOnsetAges ??= new Dictionary<int, int>();
        lastMenopauseRollAges ??= new Dictionary<int, int>();
        deferredMenopauseChecks ??= new List<Pawn>();
        postpartumCare ??= new Dictionary<int, CareRecord>();
    }

    internal bool HasDevelopedFertilityCycle(Pawn pawn)
    {
        if (pawn?.ageTracker == null)
        {
            return false;
        }

        int age = pawn.ageTracker.AgeBiologicalYears;
        if (age < FertilityCycles.MinimumCycleAge)
        {
            return false;
        }

        if (age >= FertilityCycles.GuaranteedCycleAge)
        {
            return true;
        }

        return cycleOnsetAges.ContainsKey(pawn.thingIDNumber)
            || pawn.health?.hediffSet?.HasHediff(FE_DefOf.FE_FertilityCycle) == true;
    }

    internal void RecordExistingFertilityCycle(Pawn pawn)
    {
        if (pawn?.ageTracker == null
            || pawn.health?.hediffSet?.HasHediff(FE_DefOf.FE_FertilityCycle) != true)
        {
            return;
        }

        int age = pawn.ageTracker.AgeBiologicalYears;
        if (age < FertilityCycles.MinimumCycleAge
            || age >= FertilityCycles.GuaranteedCycleAge)
        {
            return;
        }

        int pawnId = pawn.thingIDNumber;
        cycleOnsetAges[pawnId] = age;
        lastCycleOnsetRollAges.Remove(pawnId);
    }

    internal bool TryDevelopFertilityCycle(Pawn pawn)
    {
        if (HasDevelopedFertilityCycle(pawn))
        {
            return true;
        }

        if (Scribe.mode != LoadSaveMode.Inactive
            || !ReproductionCompat.CanExperienceFertilityCycle(pawn))
        {
            return false;
        }

        int age = pawn.ageTracker.AgeBiologicalYears;
        if (age < FertilityCycles.MinimumCycleAge)
        {
            return false;
        }

        if (age >= FertilityCycles.GuaranteedCycleAge)
        {
            return true;
        }

        int pawnId = pawn.thingIDNumber;
        int firstRollAge = FertilityCycles.MinimumCycleAge;
        if (lastCycleOnsetRollAges.TryGetValue(pawnId, out int lastRollAge))
        {
            if (lastRollAge >= age)
            {
                return false;
            }

            firstRollAge = lastRollAge + 1;
        }

        for (int rollAge = firstRollAge; rollAge <= age; rollAge++)
        {
            if (!Rand.Chance(FertilityCycles.CycleOnsetChanceAtAge(rollAge)))
            {
                continue;
            }

            cycleOnsetAges[pawnId] = rollAge;
            lastCycleOnsetRollAges.Remove(pawnId);
            return true;
        }

        lastCycleOnsetRollAges[pawnId] = age;
        return false;
    }

    internal void RegisterPostpartumCareWindow(Pawn pawn)
    {
        RegisterPostpartumCareWindow(pawn, Find.TickManager.TicksGame);
    }

    internal void RegisterPostpartumCareWindow(Pawn pawn, int birthTick)
    {
        if (pawn == null)
        {
            return;
        }

        int pawnId = pawn.thingIDNumber;
        if (!postpartumCare.TryGetValue(pawnId, out CareRecord record) || record?.birthTick != birthTick)
        {
            postpartumCare[pawnId] = new CareRecord(pawn, birthTick);
        }
        else
        {
            record.birther = pawn;
        }
    }

    internal void RegisterPostpartumNewborn(Pawn birther, Pawn baby)
    {
        if (baby == null || baby.Dead || baby.Destroyed || baby.RaceProps?.Humanlike != true
            || !TryGetPostpartumCareWindow(birther, out CareRecord record, out _))
        {
            return;
        }

        if (!record.babies.Contains(baby))
        {
            record.babies.Add(baby);
        }
    }

    internal bool TryGetPostpartumCareWindow(Pawn pawn, out CareRecord record, out int ageTicks)
    {
        record = null;
        ageTicks = 0;
        if (pawn == null || !postpartumCare.TryGetValue(pawn.thingIDNumber, out record) || record == null)
        {
            return false;
        }

        ageTicks = Find.TickManager.TicksGame - record.birthTick;
        if (ageTicks < 0 || ageTicks > PostpartumCare.CareCheckEndTicks)
        {
            postpartumCare.Remove(pawn.thingIDNumber);
            record = null;
            ageTicks = 0;
            return false;
        }

        record.birther = pawn;
        return true;
    }

    internal CareRecord FindPostpartumCare(Pawn recipient)
    {
        if (recipient == null)
        {
            return null;
        }

        if (TryGetPostpartumCareWindow(recipient, out CareRecord care, out _))
        {
            return care;
        }

        int currentTick = Find.TickManager.TicksGame;
        foreach (CareRecord record in postpartumCare.Values)
        {
            if (record != null && record.babies.Contains(recipient)
                && currentTick >= record.birthTick
                && currentTick - record.birthTick <= PostpartumCare.CareCheckEndTicks)
            {
                return record;
            }
        }

        return null;
    }

    private void ReconcilePostpartumCare()
    {
        List<Pawn> pawns = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive;
        for (int i = 0; i < pawns.Count; i++)
        {
            PostpartumCare.RegisterRecovery(pawns[i]);
        }
    }

    public void RegisterFosterPlacement(Pawn child, Faction sourceFaction)
    {
        if (child == null)
        {
            return;
        }

        for (int i = 0; i < fosterPlacements.Count; i++)
        {
            if (fosterPlacements[i]?.child == child)
            {
                fosterPlacements[i].sourceFaction = sourceFaction;
                return;
            }
        }

        fosterPlacements.Add(new FosterPlacementRecord(child, sourceFaction));
    }

    public void RemoveFosterPlacement(Pawn child)
    {
        for (int i = fosterPlacements.Count - 1; i >= 0; i--)
        {
            if (fosterPlacements[i]?.child == child)
            {
                fosterPlacements.RemoveAt(i);
            }
        }
    }

    public bool HasActiveFosterPlacement(Pawn child)
    {
        return TryGetActiveFosterPlacement(child, out _);
    }

    public bool TryGetActiveFosterPlacement(Pawn child, out FosterPlacementRecord placement)
    {
        for (int i = 0; i < fosterPlacements.Count; i++)
        {
            FosterPlacementRecord current = fosterPlacements[i];
            if (current?.child == child && current.Active)
            {
                placement = current;
                return true;
            }
        }

        placement = null;
        return false;
    }

    internal void NotifyBiologicalBirthday(Pawn pawn, int birthdayAge)
    {
        if (pawn?.RaceProps?.Humanlike != true
            || birthdayAge < FosterResolution.ResolutionAge)
        {
            return;
        }

        UpdateDeferredMenopauseCheck(
            pawn,
            Menopause.CheckAtBirthday(pawn, birthdayAge, lastMenopauseRollAges));
        TrySendFosterPlacementReview(pawn);
    }

    internal void NotifyPawnSpawned(Pawn pawn)
    {
        if (pawn?.RaceProps?.Humanlike != true)
        {
            return;
        }

        UpdateDeferredMenopauseCheck(
            pawn,
            Menopause.Reconcile(pawn, lastMenopauseRollAges));
        TrySendFosterPlacementReview(pawn);
    }

    internal void ReconcileMenopauseForActivePawns()
    {
        List<Pawn> pawns = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive;
        for (int i = 0; i < pawns.Count; i++)
        {
            Pawn pawn = pawns[i];
            if (pawn?.RaceProps?.Humanlike != true)
            {
                continue;
            }

            UpdateDeferredMenopauseCheck(
                pawn,
                Menopause.Reconcile(pawn, lastMenopauseRollAges));
        }
    }

    public override void GameComponentTick()
    {
        base.GameComponentTick();

        int ticksGame = Find.TickManager.TicksGame;
        if (ticksGame % 2500 != 97)
        {
            return;
        }

        if (!ModsConfig.BiotechActive)
        {
            return;
        }

        bool maintainRooLactation = MinotaurCompat.CanMaintainGeneLactation();
        List<Map> maps = Find.Maps;
        for (int mapIndex = 0; mapIndex < maps.Count; mapIndex++)
        {
            Map map = maps[mapIndex];
            List<Pawn> pawns = map.mapPawns.AllHumanlikeSpawned;
            for (int pawnIndex = pawns.Count - 1; pawnIndex >= 0; pawnIndex--)
            {
                Pawn pawn = pawns[pawnIndex];
                if (pawn == null || pawn.Destroyed || !pawn.Spawned)
                {
                    continue;
                }

                if (maintainRooLactation)
                {
                    MinotaurCompat.EnsureLactation(pawn);
                }

                FertilityCycles.EnsureCycle(pawn);
            }
        }

        if (maintainRooLactation)
        {
            MinotaurCompat.SynchronizeTravellingPawns();
        }

        CheckDeferredMenopauseChecks();

        if (ticksGame % GenDate.TicksPerDay == 97)
        {
            CleanupReproductiveAgeHistory();
            CleanupFosterPlacements();
        }

        UpdatePostpartumCare(ticksGame);
        CheckFosterPlacementReviews();
        CheckOrphanAdoptionPrompts();
        CleanupOrphanAdoptions();
    }

    private void CleanupReproductiveAgeHistory()
    {
        if (lastCycleOnsetRollAges.Count == 0
            && cycleOnsetAges.Count == 0
            && lastMenopauseRollAges.Count == 0)
        {
            return;
        }

        HashSet<int> retainedPawnIds = new HashSet<int>();
        HashSet<int> retainedUnderEighteenPawnIds = new HashSet<int>();
        List<Pawn> retainedPawns = PawnsFinder.All_AliveOrDead;
        for (int i = 0; i < retainedPawns.Count; i++)
        {
            Pawn pawn = retainedPawns[i];
            if (pawn != null)
            {
                retainedPawnIds.Add(pawn.thingIDNumber);
                if (pawn.ageTracker.AgeBiologicalYears < FertilityCycles.GuaranteedCycleAge)
                {
                    retainedUnderEighteenPawnIds.Add(pawn.thingIDNumber);
                }
            }
        }

        RemoveStaleAgeRecords(lastCycleOnsetRollAges, retainedUnderEighteenPawnIds);
        RemoveStaleAgeRecords(cycleOnsetAges, retainedUnderEighteenPawnIds);
        RemoveStaleAgeRecords(lastMenopauseRollAges, retainedPawnIds);
    }

    private static void RemoveStaleAgeRecords(Dictionary<int, int> records, HashSet<int> retainedPawnIds)
    {
        List<int> stalePawnIds = null;
        foreach (int pawnId in records.Keys)
        {
            if (!retainedPawnIds.Contains(pawnId))
            {
                stalePawnIds ??= new List<int>();
                stalePawnIds.Add(pawnId);
            }
        }

        if (stalePawnIds == null)
        {
            return;
        }

        for (int i = 0; i < stalePawnIds.Count; i++)
        {
            records.Remove(stalePawnIds[i]);
        }
    }

    private void UpdatePostpartumCare(int currentTick)
    {
        List<int> expiredPawnIds = null;
        foreach (KeyValuePair<int, CareRecord> entry in postpartumCare)
        {
            int ageTicks = entry.Value == null ? -1 : currentTick - entry.Value.birthTick;
            if (ageTicks >= 0 && ageTicks <= PostpartumCare.CareCheckEndTicks
                && entry.Value.birther?.Dead != true && entry.Value.birther?.Destroyed != true)
            {
                PostpartumCare.EvaluatePostpartumCare(entry.Value, currentTick);
                continue;
            }

            expiredPawnIds ??= new List<int>();
            expiredPawnIds.Add(entry.Key);
        }

        if (expiredPawnIds == null)
        {
            return;
        }

        for (int i = 0; i < expiredPawnIds.Count; i++)
        {
            postpartumCare.Remove(expiredPawnIds[i]);
        }
    }

    private void CheckFosterPlacementReviews()
    {
        for (int i = fosterPlacements.Count - 1; i >= 0; i--)
        {
            FosterPlacementRecord placement = fosterPlacements[i];
            TrySendFosterPlacementReview(placement);
            if (placement?.Active != true)
            {
                continue;
            }

            if (placement.reviewLetterSent)
            {
                int now = Find.TickManager.TicksGame;
                if (placement.reviewDeadlineTick < 0)
                {
                    placement.reviewDeadlineTick = now + FosterResolution.ReviewDurationTicks;
                }
                if (now >= placement.reviewDeadlineTick)
                {
                    FosterResolution.TryAdoptFosterChild(placement.child, placement.sourceFaction);
                }
            }
            else
            {
                FosterResolution.CheckGuardian(placement);
            }
        }
    }

    private void TrySendFosterPlacementReview(Pawn child)
    {
        if (TryGetActiveFosterPlacement(child, out FosterPlacementRecord placement))
        {
            TrySendFosterPlacementReview(placement);
        }
    }

    private static void TrySendFosterPlacementReview(FosterPlacementRecord placement)
    {
        if (placement?.ReadyForReview == true)
        {
            placement.reviewLetterSent = FosterResolution.SendReviewLetter(
                placement.child,
                placement.sourceFaction);
        }
    }

    private void UpdateDeferredMenopauseCheck(Pawn pawn, MenopauseCheckResult result)
    {
        if (pawn == null)
        {
            return;
        }

        if (result == MenopauseCheckResult.Deferred)
        {
            if (!deferredMenopauseChecks.Contains(pawn))
            {
                deferredMenopauseChecks.Add(pawn);
            }

            return;
        }

        deferredMenopauseChecks.Remove(pawn);
    }

    private void CheckDeferredMenopauseChecks()
    {
        for (int i = deferredMenopauseChecks.Count - 1; i >= 0; i--)
        {
            Pawn pawn = deferredMenopauseChecks[i];
            if (pawn == null
                || pawn.Destroyed
                || pawn.Dead
                || Menopause.Reconcile(pawn, lastMenopauseRollAges) != MenopauseCheckResult.Deferred)
            {
                deferredMenopauseChecks.RemoveAt(i);
            }
        }
    }

    private void CleanupFosterPlacements()
    {
        for (int i = fosterPlacements.Count - 1; i >= 0; i--)
        {
            FosterPlacementRecord placement = fosterPlacements[i];
            if (placement == null || !placement.Active)
            {
                fosterPlacements.RemoveAt(i);
            }
        }
    }

    private void CheckOrphanAdoptionPrompts()
    {
        int tick = Find.TickManager.TicksGame;
        List<Pawn> pawns = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists;
        for (int i = 0; i < pawns.Count; i++)
        {
            Pawn child = pawns[i];
            if (!OrphanAdoption.IsEligibleOrphanChild(child) || HasActiveFosterPlacement(child))
            {
                continue;
            }

            OrphanAdoptionRecord record = GetOrCreateOrphanAdoptionRecord(child, tick);
            if (tick < record.nextPromptTick)
            {
                continue;
            }

            if (!OrphanAdoption.IsStablePromptLocation(child))
            {
                continue;
            }

            if (OrphanAdoption.TrySendPrompt(child, out _))
            {
                record.nextPromptTick = tick + OrphanAdoption.ReminderCooldownTicks;
            }
            else
            {
                record.nextPromptTick = tick + OrphanAdoption.GracePeriodTicks;
            }
        }
    }

    private OrphanAdoptionRecord GetOrCreateOrphanAdoptionRecord(Pawn child, int currentTick)
    {
        for (int i = 0; i < orphanAdoptions.Count; i++)
        {
            OrphanAdoptionRecord record = orphanAdoptions[i];
            if (record?.child == child)
            {
                return record;
            }
        }

        OrphanAdoptionRecord newRecord = new OrphanAdoptionRecord(child, currentTick);
        orphanAdoptions.Add(newRecord);
        return newRecord;
    }

    internal void NotifyOrphanAdoptionPromptSent(Pawn child)
    {
        if (child == null)
        {
            return;
        }

        int currentTick = Find.TickManager.TicksGame;
        OrphanAdoptionRecord record = GetOrCreateOrphanAdoptionRecord(child, currentTick);
        int nextPromptTick = currentTick + OrphanAdoption.ReminderCooldownTicks;
        if (record.nextPromptTick < nextPromptTick)
        {
            record.nextPromptTick = nextPromptTick;
        }
    }

    private void CleanupOrphanAdoptions()
    {
        for (int i = orphanAdoptions.Count - 1; i >= 0; i--)
        {
            OrphanAdoptionRecord record = orphanAdoptions[i];
            if (record?.child == null
                || !OrphanAdoption.IsEligibleOrphanChild(record.child)
                || HasActiveFosterPlacement(record.child))
            {
                orphanAdoptions.RemoveAt(i);
            }
        }
    }
}
