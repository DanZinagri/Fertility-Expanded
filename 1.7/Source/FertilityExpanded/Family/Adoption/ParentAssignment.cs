using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FertilityExpanded;

public static class ParentAssignment
{
    private const string KinshipBloodlinePrimacyDefName = "FE_Kinship_BloodlinePrimacy";

    private static PreceptDef kinshipBloodlinePrimacy;

    public static bool CanChooseAdoptiveParentForVanillaAdoption(Pawn child)
    {
        if (!IsValidIncomingAdoptionChild(child) || ParentRelationDef(ParentKind.Adoptive) == null)
        {
            return false;
        }

        foreach (Pawn parent in ValidParentCandidates())
        {
            if (parent != child)
            {
                return true;
            }
        }

        return false;
    }

    public static void OpenVanillaAdoptionParentPicker(Pawn child, Action<Pawn> completeAdoption)
    {
        if (!IsValidIncomingAdoptionChild(child) || completeAdoption == null)
        {
            return;
        }

        List<FloatMenuOption> options = new List<FloatMenuOption>();
        foreach (Pawn candidate in ValidParentCandidates())
        {
            if (candidate == child)
            {
                continue;
            }

            Pawn parent = candidate;
            options.Add(new FloatMenuOption(
                "FE_AdoptAsChildOf".Translate(parent.Named("PARENT"), child.Named("CHILD")).Resolve(),
                delegate
                {
                    completeAdoption(parent);
                }));
        }

        if (options.Count == 0)
        {
            completeAdoption(null);
            return;
        }

        options.Add(new FloatMenuOption(
            "FE_AdoptWithoutAssignedParent".Translate(child.Named("CHILD")).Resolve(),
            delegate
            {
                completeAdoption(null);
            }));

        Find.WindowStack.Add(new FloatMenu(options));
    }

    public static bool SendAssignmentLetter(Pawn child, ParentKind kind, ParentReason reason = ParentReason.Placement)
    {
        if (!IsValidChild(child) || HasBlockingParentAssignment(child, ParentRelationDef(kind), kind))
        {
            return false;
        }

        foreach (Letter pending in Find.LetterStack.LettersListForReading)
        {
            if (pending is ParentAssignmentLetter && pending.CanShowInLetterStack
                && pending.lookTargets?.TryGetPrimaryTarget().Thing == child)
            {
                return true;
            }
        }

        TaggedString label = LabelFor(kind, child, reason);
        TaggedString text = TextFor(kind, child, reason);
        ParentAssignmentLetter letter = LetterMaker.MakeLetter(
            label,
            text,
            FE_DefOf.FE_AssignFamilyParentLetter,
            child) as ParentAssignmentLetter;

        if (letter == null)
        {
            return false;
        }

        letter.Configure(child, kind, reason);
        Find.LetterStack.ReceiveLetter(letter);
        return true;
    }

    public static bool TryAssignParent(Pawn child, Pawn parent, ParentKind kind)
    {
        if (!IsValidChild(child) || !IsValidParent(parent) || child == parent)
        {
            return false;
        }

        PawnRelationDef relationDef = ParentRelationDef(kind);
        if (relationDef == null)
        {
            return false;
        }

        if (kind == ParentKind.Adoptive
            ? FamilyRelations.HasAdoptiveParentRelation(child, parent)
            : child.relations.DirectRelationExists(relationDef, parent))
        {
            return true;
        }

        if (HasBlockingParentAssignment(child, relationDef, kind))
        {
            return false;
        }

        child.relations.AddDirectRelation(relationDef, parent);
        ApplyAssignmentThoughts(child, parent, kind);
        Messages.Message(MessageFor(kind, child, parent), new LookTargets(child, parent), MessageTypeDefOf.PositiveEvent);
        return true;
    }

    internal static bool TryAssignExternalAdoptiveParent(Pawn child, Pawn parent)
    {
        if (child == null
            || parent == null
            || child == parent
            || child.Destroyed
            || child.Dead
            || parent.Destroyed
            || parent.Dead
            || child.RaceProps?.Humanlike != true
            || parent.RaceProps?.Humanlike != true
            || child.relations == null
            || parent.relations == null
            || (parent.DevelopmentalStage & DevelopmentalStage.Adult) == 0)
        {
            return false;
        }

        PawnRelationDef relationDef = FE_DefOf.FE_AdoptiveParent;
        if (relationDef == null)
        {
            return false;
        }

        if (FamilyRelations.HasAdoptiveParentRelation(child, parent))
        {
            return true;
        }

        child.relations.AddDirectRelation(relationDef, parent);
        ApplyAssignmentThoughts(child, parent, ParentKind.Adoptive);
        return true;
    }

    public static bool CanAssignAnyParent(Pawn child, ParentKind kind)
    {
        if (!IsValidChild(child) || ParentRelationDef(kind) == null)
        {
            return false;
        }

        PawnRelationDef relationDef = ParentRelationDef(kind);
        if (HasBlockingParentAssignment(child, relationDef, kind))
        {
            return false;
        }

        foreach (Pawn parent in ValidParentCandidates())
        {
            if (parent != child)
            {
                return true;
            }
        }

        return false;
    }

    public static IEnumerable<Pawn> ValidParentCandidates()
    {
        foreach (Pawn pawn in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists)
        {
            if (IsValidParent(pawn))
            {
                yield return pawn;
            }
        }
    }

    private static bool IsValidChild(Pawn child)
    {
        return ModsConfig.BiotechActive
            && child != null
            && !child.Destroyed
            && !child.Dead
            && child.Faction == Faction.OfPlayer
            && child.RaceProps.Humanlike
            && child.relations != null;
    }

    private static bool IsValidIncomingAdoptionChild(Pawn child)
    {
        PawnRelationDef relationDef = ParentRelationDef(ParentKind.Adoptive);
        return ModsConfig.BiotechActive
            && child != null
            && !child.Destroyed
            && !child.Dead
            && child.Faction != Faction.OfPlayer
            && child.RaceProps.Humanlike
            && child.relations != null
            && child.AdoptableBy(Faction.OfPlayer)
            && relationDef != null
            && !FamilyRelations.HasAnyAdoptiveParentRelation(child);
    }

    private static bool IsValidParent(Pawn parent)
    {
        return parent != null
            && !parent.Destroyed
            && !parent.Dead
            && parent.Faction == Faction.OfPlayer
            && parent.RaceProps.Humanlike
            && (parent.DevelopmentalStage & DevelopmentalStage.Adult) != 0
            && parent.relations != null;
    }

    private static PawnRelationDef ParentRelationDef(ParentKind kind)
    {
        return kind == ParentKind.Foster ? FE_DefOf.FE_FosterParent : FE_DefOf.FE_AdoptiveParent;
    }

    internal static bool HasBlockingParentAssignment(Pawn child, PawnRelationDef relationDef, ParentKind kind)
    {
        if (relationDef == null)
        {
            return true;
        }

        if (kind == ParentKind.Adoptive)
        {
            return FamilyRelations.HasLivingAdoptiveParentRelation(child);
        }

        return FosterResolution.HasLivingFosterParent(child);
    }

    private static TaggedString LabelFor(ParentKind kind, Pawn child, ParentReason reason)
    {
        if (kind == ParentKind.Foster)
        {
            return "FE_AssignFosterParentLabel".Translate(child.Named("CHILD"));
        }

        return reason == ParentReason.Orphaned
            ? "FE_OrphanAdoptionLetterLabel".Translate(child.Named("CHILD"))
            : "FE_AssignAdoptiveParentLabel".Translate(child.Named("CHILD"));
    }

    private static TaggedString TextFor(ParentKind kind, Pawn child, ParentReason reason)
    {
        if (kind == ParentKind.Foster)
        {
            return "FE_AssignFosterParentText".Translate(child.Named("CHILD"));
        }

        return reason == ParentReason.Orphaned
            ? "FE_OrphanAdoptionLetterText".Translate(child.Named("CHILD"))
            : "FE_AssignAdoptiveParentText".Translate(child.Named("CHILD"));
    }

    private static TaggedString MessageFor(ParentKind kind, Pawn child, Pawn parent)
    {
        return kind == ParentKind.Foster
            ? "FE_FosterParentAssigned".Translate(parent.Named("PARENT"), child.Named("CHILD"))
            : "FE_AdoptiveParentAssigned".Translate(parent.Named("PARENT"), child.Named("CHILD"));
    }

    private static void ApplyAssignmentThoughts(Pawn child, Pawn parent, ParentKind kind)
    {
        if (parent.needs?.mood?.thoughts?.memories != null)
        {
            if (kind == ParentKind.Foster)
            {
                if (Features.FosterParentWelcomeMemory)
                {
                    parent.needs.mood.thoughts.memories.TryGainMemory(FE_DefOf.FE_FosterChildWelcomed, child);
                }
            }
            else if (PawnHasPrecept(parent, KinshipBloodlinePrimacy))
            {
                parent.needs.mood.thoughts.memories.TryGainMemory(FE_DefOf.FE_AdoptionBloodlineTension, child);
            }
            else if (Features.AdoptiveParentWelcomeMemory)
            {
                ThoughtDef thought = AdoptionMemoryFor(parent);
                if (thought != null)
                {
                    parent.needs.mood.thoughts.memories.TryGainMemory(thought, child);
                }
            }
        }

        if (kind == ParentKind.Foster
            && Features.FosterChildBondMemory
            && child.needs?.mood?.thoughts?.memories != null)
        {
            child.needs.mood.thoughts.memories.TryGainMemory(FE_DefOf.FE_FosterBond, parent);
        }
    }

    private static bool PawnHasPrecept(Pawn pawn, PreceptDef preceptDef)
    {
        return ModsConfig.IdeologyActive && preceptDef != null && pawn?.Ideo?.HasPrecept(preceptDef) == true;
    }

    internal static ThoughtDef AdoptionMemoryFor(Pawn parent)
    {
        if (!ModsConfig.IdeologyActive || parent.Ideo == null || Find.IdeoManager?.classicMode == true)
        {
            return FE_DefOf.FE_AdoptedChildWelcomed;
        }

        if (FamilyPrecepts.HasPrecept(parent, "FE_Kinship_AdoptiveKinship"))
        {
            return FE_DefOf.FE_AdoptiveKinshipWelcomed;
        }

        return FamilyPrecepts.HasPrecept(parent, "FE_Kinship_AdoptionAccepted")
            ? FE_DefOf.FE_AdoptedChildWelcomed
            : null;
    }

    private static PreceptDef KinshipBloodlinePrimacy => kinshipBloodlinePrimacy ??= DefDatabase<PreceptDef>.GetNamedSilentFail(KinshipBloodlinePrimacyDefName);
}

public enum ParentKind
{
    Adoptive,
    Foster
}

public enum ParentReason
{
    Placement,
    Orphaned
}
