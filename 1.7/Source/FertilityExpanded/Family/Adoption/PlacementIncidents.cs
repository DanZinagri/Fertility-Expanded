using RimWorld;
using Verse;

namespace FertilityExpanded;

public abstract class PlacementIncident : IncidentWorker
{
    protected abstract ParentKind AssignmentKind { get; }

    protected override bool CanFireNowSub(IncidentParms parms)
    {
        if (!Features.IsFamilyPlacementEnabled(AssignmentKind)
            || !base.CanFireNowSub(parms)
            || !ModsConfig.BiotechActive
            || !Find.Storyteller.difficulty.ChildrenAllowed)
        {
            return false;
        }

        if (parms.target is not Map map || !map.IsPlayerHome || map.mapPawns.FreeColonistsSpawnedCount == 0)
        {
            return false;
        }

        return Find.FactionManager.RandomNonHostileFaction(allowHidden: false, allowDefeated: false, allowNonHumanlike: false) != null;
    }

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        if (!Features.IsFamilyPlacementEnabled(AssignmentKind)
            || parms.target is not Map map)
        {
            return false;
        }

        Faction sourceFaction = Find.FactionManager.RandomNonHostileFaction(allowHidden: false, allowDefeated: false, allowNonHumanlike: false);
        FamilyPlacement.SendPlacementLetter(map, AssignmentKind, sourceFaction);
        return true;
    }
}

public class AdoptionIncident : PlacementIncident
{
    protected override ParentKind AssignmentKind => ParentKind.Adoptive;
}

public class FosterIncident : PlacementIncident
{
    protected override ParentKind AssignmentKind => ParentKind.Foster;
}
