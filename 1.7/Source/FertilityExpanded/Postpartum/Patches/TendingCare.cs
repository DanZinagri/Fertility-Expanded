using HarmonyLib;
using RimWorld;
using Verse;

namespace FertilityExpanded.Patches;

[HarmonyPatch(typeof(TendUtility), nameof(TendUtility.DoTend))]
public static class TendingCarePatch
{
    public static void Prefix(Pawn doctor, Pawn patient, out int __state)
    {
        __state = PostpartumCare.CareRecipient(doctor, patient) != null && patient.records != null
            ? patient.records.GetAsInt(RecordDefOf.TimesTendedTo)
            : -1;
    }

    public static void Postfix(Pawn doctor, Pawn patient, int __state)
    {
        if (__state >= 0 && patient.records.GetAsInt(RecordDefOf.TimesTendedTo) > __state)
        {
            PostpartumCare.NotifyCareCompleted(doctor, patient);
        }
    }
}
