using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace FertilityExpanded;

public static class BirthObligations
{
    private const string NewLifeCelebrationRitualDefName = "FE_NewLifeCelebrationRitual";
    private const int DesiredOpportunityTicks = 3 * GenDate.TicksPerDay;

    private static PreceptDef newLifeCelebrationRitualDef;

    public static bool TryRegisterNewLifeRitual(Pawn birther, Pawn baby)
    {
        return TryRegisterNewLifeRitual(birther, new[] { baby });
    }

    public static bool TryRegisterNewLifeRitual(Pawn birther, IReadOnlyList<Pawn> newborns)
    {
        if (!ModsConfig.IdeologyActive || newborns == null)
        {
            return false;
        }

        Precept_Ritual ritual = null;
        List<Pawn> babies = new List<Pawn>();
        for (int i = 0; i < newborns.Count; i++)
        {
            Pawn baby = newborns[i];
            if (!RitualNewborns.Living(baby) || baby.Faction != Faction.OfPlayer || babies.Contains(baby))
            {
                continue;
            }

            ritual ??= FindRitualFor(birther, baby);
            babies.Add(baby);
        }

        if (ritual == null)
        {
            return false;
        }
        for (int i = babies.Count - 1; i >= 0; i--)
        {
            if (RitualAlreadyRegistered(ritual, babies[i]))
            {
                babies.RemoveAt(i);
            }
        }

        bool registered = false;
        for (int i = 0; i < babies.Count; i += 3)
        {
            registered |= RegisterGroup(ritual, babies, i);
        }
        return registered;
    }

    private static bool RegisterGroup(Precept_Ritual ritual, List<Pawn> babies, int first)
    {
        RitualObligation obligation = new RitualObligation(ritual, new TargetInfo(babies[first]),
            first + 1 < babies.Count ? new TargetInfo(babies[first + 1]) : TargetInfo.Invalid,
            first + 2 < babies.Count ? new TargetInfo(babies[first + 2]) : TargetInfo.Invalid);
        obligation.DebugOffsetTriggeredTick(DesiredOpportunityTicks - RitualObligation.DaysToExpire * GenDate.TicksPerDay);
        obligation.sendLetter = false;
        ritual.AddObligation(obligation);
        if (ritual.activeObligations == null || !ritual.activeObligations.Contains(obligation))
        {
            return false;
        }

        SendOpportunityLetter(ritual, obligation, babies[first]);
        return true;
    }

    public static bool HasActiveNewLifeRitual(Pawn birther, Pawn baby)
    {
        if (!ModsConfig.IdeologyActive || baby == null || baby.Destroyed || baby.Dead || baby.Faction != Faction.OfPlayer)
        {
            return false;
        }

        Precept_Ritual ritual = FindRitualFor(birther, baby);
        return ritual != null && RitualAlreadyRegistered(ritual, baby);
    }

    private static Precept_Ritual FindRitualFor(Pawn birther, Pawn baby)
    {
        PreceptDef ritualDef = NewLifeCelebrationRitualDef;
        if (ritualDef == null)
        {
            return null;
        }

        Precept_Ritual ritual = RitualFromIdeo(birther?.Ideo, ritualDef);
        ritual ??= RitualFromIdeo(baby?.Ideo, ritualDef);

        Faction playerFaction = Faction.OfPlayerSilentFail;
        if (ritual == null && playerFaction?.ideos?.PrimaryIdeo != null)
        {
            ritual = RitualFromIdeo(playerFaction.ideos.PrimaryIdeo, ritualDef);
        }

        return ritual;
    }

    private static Precept_Ritual RitualFromIdeo(Ideo ideo, PreceptDef ritualDef)
    {
        if (ideo == null)
        {
            return null;
        }

        return ideo.GetPrecept(ritualDef) as Precept_Ritual;
    }

    private static bool RitualAlreadyRegistered(Precept_Ritual ritual, Pawn baby)
    {
        List<RitualObligation> obligations = ritual.activeObligations;
        if (obligations == null)
        {
            return false;
        }

        for (int i = 0; i < obligations.Count; i++)
        {
            RitualObligation obligation = obligations[i];
            if (RitualNewborns.Contains(obligation, baby) && obligation.StillValid)
            {
                return true;
            }
        }

        return false;
    }

    private static void SendOpportunityLetter(Precept_Ritual ritual, RitualObligation obligation, Pawn baby)
    {
        TaggedString label = obligation.LetterLabel;
        TaggedString text = "FE_NewLifeRitualGroupOpportunityText".Translate(
            RitualNewborns.Labels(obligation).Named("BABIES"),
            ritual.LabelCap.Named("RITUAL"),
            DesiredOpportunityTicks.ToStringTicksToPeriodVague().Named("DURATION"));
        Find.LetterStack.ReceiveLetter(label, text.Resolve(), LetterDefOf.NeutralEvent, LetterLookTargets(ritual, obligation, baby));
    }

    private static LookTargets LetterLookTargets(Precept_Ritual ritual, RitualObligation obligation, Pawn baby)
    {
        Map map = baby.MapHeld ?? Find.AnyPlayerHomeMap;
        if (map != null)
        {
            foreach (TargetInfo target in ritual.obligationTargetFilter.GetTargets(obligation, map))
            {
                return (LookTargets)target;
            }
        }

        return baby != null ? (LookTargets)baby : LookTargets.Invalid;
    }

    private static PreceptDef NewLifeCelebrationRitualDef
    {
        get
        {
            newLifeCelebrationRitualDef ??= DefDatabase<PreceptDef>.GetNamedSilentFail(NewLifeCelebrationRitualDefName);
            return newLifeCelebrationRitualDef;
        }
    }
}
