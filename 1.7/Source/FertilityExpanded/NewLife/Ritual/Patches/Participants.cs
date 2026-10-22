using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace FertilityExpanded;

[HarmonyPatch(typeof(Dialog_BeginRitual), MethodType.Constructor, new System.Type[] {
    typeof(string), typeof(Precept_Ritual), typeof(TargetInfo), typeof(Map),
    typeof(Dialog_BeginRitual.ActionCallback), typeof(Pawn), typeof(RitualObligation),
    typeof(Dialog_BeginRitual.PawnFilter), typeof(string), typeof(List<Pawn>),
    typeof(Dictionary<string, Pawn>), typeof(RitualOutcomeEffectDef), typeof(List<string>), typeof(Pawn) })]
public static class NewLifeParticipantsPatch
{
    public static void Prefix(Precept_Ritual ritual, RitualObligation obligation, ref Dialog_BeginRitual.PawnFilter filter)
    {
        if (ritual?.def.defName != "FE_NewLifeCelebrationRitual")
        {
            return;
        }

        Dialog_BeginRitual.PawnFilter original = filter;
        filter = (pawn, voluntary, allowOtherIdeos) => !RitualNewborns.IsCarrier(obligation, pawn)
            && !RitualNewborns.Contains(obligation, pawn)
            && (original == null || original(pawn, voluntary, allowOtherIdeos));
    }

    public static void Postfix(Precept_Ritual ritual, RitualObligation obligation, RitualRoleAssignments ___assignments)
    {
        if (ritual?.def.defName != "FE_NewLifeCelebrationRitual" || ___assignments == null)
        {
            return;
        }

        List<RitualRole> roles = ___assignments.AllRolesForReading;
        for (int i = 0; i < roles.Count; i++)
        {
            if (roles[i] is WelcomeParent guardian)
            {
                roles[i] = guardian.ForBirth(obligation);
            }
        }
    }
}
