using System.Collections.Generic;
using FertilityExpanded.Patches.Compatibility;
using RimWorld;
using Verse;

namespace FertilityExpanded;

public static class ChildHarm
{
    private static readonly List<DirectPawnRelation> TmpRelations = new List<DirectPawnRelation>();

    public static void NotifyChildDamaged(Pawn child, DamageInfo dinfo, float totalDamageDealt)
    {
        if (!Features.ChildHarmMemories
            || !ModsConfig.BiotechActive
            || child?.relations == null
            || child.RaceProps?.Humanlike != true
            || totalDamageDealt <= 0f)
        {
            return;
        }

        if ((child.DevelopmentalStage & (DevelopmentalStage.Newborn | DevelopmentalStage.Baby | DevelopmentalStage.Child)) == 0)
        {
            return;
        }

        if (dinfo.Def == null || !dinfo.Def.ExternalViolenceFor(child))
        {
            return;
        }

        Pawn attacker = dinfo.Instigator as Pawn;
        if (attacker == null || attacker == child)
        {
            return;
        }

        TmpRelations.Clear();
        TmpRelations.AddRange(child.relations.DirectRelations);
        for (int i = 0; i < TmpRelations.Count; i++)
        {
            DirectPawnRelation relation = TmpRelations[i];
            if (!FamilyRelations.IsParentLikeRelation(relation.def))
            {
                continue;
            }

            Pawn parent = relation.otherPawn;
            if (parent?.needs?.mood?.thoughts?.memories == null
                || parent.Dead
                || parent == attacker)
            {
                continue;
            }

            MemoryThoughtHandler memories = parent.needs.mood.thoughts.memories;
            memories.TryGainMemory(FE_DefOf.FE_ChildHarmed, child);
            if (!ModDetection.FamilyIsEverythingActive
                && attacker.RaceProps?.Humanlike == true)
            {
                memories.TryGainMemory(FE_DefOf.FE_HurtMyChild, attacker);
            }
        }
        TmpRelations.Clear();
    }

}
