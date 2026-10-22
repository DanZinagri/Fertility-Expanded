using System.Collections.Generic;
using Verse;

namespace FertilityExpanded;

internal class CareRecord : IExposable
{
    public Pawn birther;
    public int birthTick;
    public List<Pawn> babies = new List<Pawn>();
    public List<Pawn> relievedParents = new List<Pawn>();
    public int lastAssessmentTick = -1;
    public int lastCareTick = -1;
    public int lastLactatingCareTick = -1;
    public int lastWetNurseCareTick = -1;
    public int goodReadings;
    public int poorReadings;

    public CareRecord()
    {
    }

    public CareRecord(Pawn birther, int birthTick)
    {
        this.birther = birther;
        this.birthTick = birthTick;
    }

    public void ExposeData()
    {
        Scribe_References.Look(ref birther, "birther");
        Scribe_Values.Look(ref birthTick, "birthTick");
        Scribe_Collections.Look(ref babies, "babies", LookMode.Reference);
        Scribe_Collections.Look(ref relievedParents, "relievedParents", LookMode.Reference);
        Scribe_Values.Look(ref lastAssessmentTick, "lastAssessmentTick", -1);
        Scribe_Values.Look(ref lastCareTick, "lastCareTick", -1);
        Scribe_Values.Look(ref lastLactatingCareTick, "lastLactatingCareTick", -1);
        Scribe_Values.Look(ref lastWetNurseCareTick, "lastWetNurseCareTick", -1);
        Scribe_Values.Look(ref goodReadings, "goodReadings");
        Scribe_Values.Look(ref poorReadings, "poorReadings");
        babies ??= new List<Pawn>();
        relievedParents ??= new List<Pawn>();
    }
}
