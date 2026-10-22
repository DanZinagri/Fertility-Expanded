using RimWorld;
using Verse;

namespace FertilityExpanded;

public static class BirthEffects
{
    public static void ApplyPostpartumState(Pawn birther, bool applyRecovery)
    {
        if (birther?.health == null || birther.Dead)
        {
            return;
        }

        Current.Game?.GetComponent<GameState>()?.RegisterPostpartumCareWindow(birther);

        if (applyRecovery && Features.PostpartumRecovery)
        {
            FertilityCycles.AddPostpartumRecovery(birther);
        }

        if (Features.PostpartumSadness
            && birther.needs?.mood?.thoughts?.memories != null
            && Rand.Chance(0.25f))
        {
            MemoryThoughtHandler memories = birther.needs.mood.thoughts.memories;
            memories.TryGainMemory(FE_DefOf.FE_PostpartumSadness);
            Thought_Memory memory = memories.GetFirstMemoryOfDef(FE_DefOf.FE_PostpartumSadness);
            if (memory != null)
            {
                memory.durationTicksOverride = Rand.RangeInclusive(2, 15) * GenDate.TicksPerDay;
            }
        }
    }

    public static void ApplyBabyBond(Pawn birther, Pawn baby)
    {
        if (!Features.BabyBondingMemories
            || birther?.needs?.mood?.thoughts?.memories == null
            || birther.Dead
            || baby == null
            || baby.Dead)
        {
            return;
        }

        birther.needs.mood.thoughts.memories.TryGainMemory(FE_DefOf.FE_BondedWithBaby, baby);
    }

    public static void ApplyFamilyEffects(Pawn birther, Pawn baby)
    {
        if (birther?.health == null || birther.Dead || baby == null || baby.Dead)
        {
            return;
        }

        PostpartumCare.TryApplySingleParentStrain(birther, baby);
        PostpartumCare.TryApplyCoparentSupport(birther, baby);
    }
}
