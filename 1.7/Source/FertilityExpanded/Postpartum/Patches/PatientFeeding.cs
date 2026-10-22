using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace FertilityExpanded.Patches;

[HarmonyPatch(typeof(JobDriver_FoodFeedPatient), "MakeNewToils")]
public static class PatientFeedingPatch
{
    public static void Postfix(JobDriver_FoodFeedPatient __instance, ref IEnumerable<Toil> __result)
    {
        Pawn patient = __instance.job.targetB.Pawn;
        if (patient?.records != null && PostpartumCare.CareRecipient(__instance.pawn, patient) != null)
        {
            __result = ObserveFeeding(__result, __instance.pawn, patient);
        }
    }

    private static IEnumerable<Toil> ObserveFeeding(IEnumerable<Toil> toils, Pawn caregiver, Pawn patient)
    {
        bool foundIngestion = false;
        foreach (Toil toil in toils)
        {
            if (toil.debugName == "FinalizeIngest" && toil.defaultCompleteMode == ToilCompleteMode.Instant
                && toil.initAction != null)
            {
                foundIngestion = true;
                Action ingest = toil.initAction;
                toil.initAction = delegate
                {
                    float nutritionBefore = patient.records.GetValue(RecordDefOf.NutritionEaten);
                    ingest();
                    if (patient.records.GetValue(RecordDefOf.NutritionEaten) > nutritionBefore)
                    {
                        PostpartumCare.NotifyCareCompleted(caregiver, patient);
                    }
                };
            }

            yield return toil;
        }

        if (!foundIngestion)
        {
            Log.WarningOnce("[Fertility Expanded] Patient feeding has no recognizable ingestion toil; postpartum care credit was not attached.", 1874932061);
        }
    }
}
