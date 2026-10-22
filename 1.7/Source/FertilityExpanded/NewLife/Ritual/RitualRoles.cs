using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace FertilityExpanded;

public class WelcomeSpeaker : RitualRoleColonist
{
    public override bool AppliesToPawn(Pawn p, out string reason, TargetInfo selectedTarget, LordJob_Ritual ritual = null, RitualRoleAssignments assignments = null, Precept_Ritual precept = null, bool skipReason = false)
    {
        if (!base.AppliesToPawn(p, out reason, selectedTarget, ritual, assignments, precept, skipReason))
        {
            return false;
        }

        Ideo ideo = (ritual?.Ritual ?? assignments?.Ritual ?? precept)?.ideo;
        Precept_Role role = p.Ideo?.GetRole(p);
        if (p.Ideo == ideo && role?.Active == true
            && (role.def.roleTags?.Contains("Leader") == true || FeedingBeliefs.IsWetNurseRolePawn(p)))
        {
            return true;
        }

        reason = skipReason ? null : "FE_NewLifeSpeakerRequirement".Translate();
        return false;
    }
}

public class WelcomeParent : RitualRoleColonist
{
    private RitualObligation birth;

    public WelcomeParent ForBirth(RitualObligation obligation)
    {
        WelcomeParent role = (WelcomeParent)MemberwiseClone();
        role.birth = obligation;
        return role;
    }

    public override bool AppliesToPawn(Pawn p, out string reason, TargetInfo selectedTarget, LordJob_Ritual ritual = null, RitualRoleAssignments assignments = null, Precept_Ritual precept = null, bool skipReason = false)
    {
        if (!base.AppliesToPawn(p, out reason, selectedTarget, ritual, assignments, precept, skipReason))
        {
            return false;
        }

        LordJob_Ritual activeRitual = ritual ?? p.GetLord()?.LordJob as LordJob_Ritual;
        RitualObligation obligation = activeRitual?.obligation ?? birth;
        if (RitualNewborns.IsParent(obligation, p))
        {
            return true;
        }

        reason = skipReason ? null : "FE_NewLifeGuardianRequirement".Translate();
        return false;
    }
}
