using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FertilityExpanded;

internal static class RitualNewborns
{
    internal static Pawn At(RitualObligation obligation, int index)
    {
        if (obligation == null)
        {
            return null;
        }

        return (index switch {
            0 => obligation.targetA.Thing,
            1 => obligation.targetB.Thing,
            2 => obligation.targetC.Thing,
            _ => null
        }) as Pawn;
    }

    internal static bool Living(Pawn baby)
    {
        return baby != null && !baby.Dead && !baby.Destroyed;
    }

    internal static bool Contains(RitualObligation obligation, Pawn baby)
    {
        return baby != null && (At(obligation, 0) == baby || At(obligation, 1) == baby || At(obligation, 2) == baby);
    }

    internal static bool IsParent(RitualObligation obligation, Pawn pawn)
    {
        for (int i = 0; i < 3; i++)
        {
            Pawn baby = At(obligation, i);
            if (Living(baby) && FamilyRelations.HasParentLikeRelation(baby, pawn))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool IsCarrier(RitualObligation obligation, Pawn pawn)
    {
        if (pawn == null)
        {
            return false;
        }

        for (int i = 0; i < 3; i++)
        {
            Pawn baby = At(obligation, i);
            if (Living(baby) && baby.CarriedBy == pawn)
            {
                return true;
            }
        }

        return false;
    }

    internal static string Labels(RitualObligation obligation)
    {
        List<string> names = new List<string>(3);
        for (int i = 0; i < 3; i++)
        {
            Pawn baby = At(obligation, i);
            if (Living(baby))
            {
                names.Add(baby.LabelShortCap);
            }
        }

        return string.Join(", ", names);
    }
}
