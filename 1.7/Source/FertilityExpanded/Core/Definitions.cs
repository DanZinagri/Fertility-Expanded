using RimWorld;
using Verse;

namespace FertilityExpanded;

[DefOf]
public static class FE_DefOf
{
    public static HediffDef FE_FertilityCycle;
    public static HediffDef FE_PostpartumRecovery;
    public static HediffDef FE_MenopauseTransition;
    public static HediffDef FE_Postmenopausal;
    public static HediffDef FE_BirthControlActive;
    public static HediffDef FE_HerbalContraception;
    public static HediffDef FE_HerbalFertilityBoost;
    public static HediffDef FE_FertilityTreatmentActive;
    public static HediffDef FE_LactationImplant;
    public static HediffDef FE_WetNurseLactationSource;

    public static ThingDef FE_HumanMilk;
    public static JobDef FE_MilkSelf;

    public static ThoughtDef FE_PostpartumSadness;
    public static ThoughtDef FE_BondedWithBaby;
    public static ThoughtDef FE_SupportedAfterBirth;
    public static ThoughtDef FE_NeglectedAfterBirth;
    public static ThoughtDef FE_SingleParentStrain;
    public static ThoughtDef FE_CoparentSupportedBirth;
    public static ThoughtDef FE_BabyCaredFor;
    public static ThoughtDef FE_WetNursedAgainstBelief;
    public static ThoughtDef FE_UsedFormulaAgainstBelief;
    public static ThoughtDef FE_UsedFormulaDespitePreference;
    public static ThoughtDef FE_FeedingDisapprovalCooldown;
    public static ThoughtDef FE_AdoptedChildWelcomed;
    public static ThoughtDef FE_AdoptiveKinshipWelcomed;
    public static ThoughtDef FE_SacredPostpartumNeglect;
    public static ThoughtDef FE_SharedChildcare;
    public static ThoughtDef FE_TerminatedPregnancyAgainstBelief;
    public static ThoughtDef FE_PerformedTerminationAgainstBelief;
    public static ThoughtDef FE_AdoptionBloodlineTension;
    public static ThoughtDef FE_FosterChildWelcomed;
    public static ThoughtDef FE_FosterBond;
    public static ThoughtDef FE_RefusedAdoptionRequest;
    public static ThoughtDef FE_ChildHarmed;
    public static ThoughtDef FE_HurtMyChild;

    public static GeneDef FE_MultipleBirthTendency;

    public static PawnCapacityDef FE_Fertility;

    public static PawnRelationDef FE_AdoptiveParent;
    public static PawnRelationDef FE_AdoptiveChild;
    public static PawnRelationDef FE_FosterParent;
    public static PawnRelationDef FE_FosterChild;

    public static QuestScriptDef FE_FosterReturnReward;

    public static LetterDef FE_AssignFamilyParentLetter;
    public static LetterDef FE_FamilyPlacementLetter;
    public static LetterDef FE_FosterResolutionLetter;

    public static HistoryEventDef FE_FamilyPlacementAccepted;
    public static HistoryEventDef FE_FosterChildReturnedHome;
    public static HistoryEventDef FE_FosterChildKept;

    static FE_DefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(FE_DefOf));
    }
}
