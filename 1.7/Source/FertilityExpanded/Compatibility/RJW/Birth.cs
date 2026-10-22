using System.Collections.Generic;
using Verse;

namespace FertilityExpanded.Patches.Compatibility;

internal static class RjwBirthPatch
{
    public static void Postfix(Hediff __instance, List<Pawn> ___babies)
    {
        Pawn mother = __instance?.pawn;
        BirthObligations.TryRegisterNewLifeRitual(mother, ___babies);
        if (mother?.health == null || mother.Dead)
        {
            return;
        }

        BirthEffects.ApplyPostpartumState(mother, applyRecovery: true);

        Pawn firstLivingBaby = null;
        if (___babies != null)
        {
            for (int i = 0; i < ___babies.Count; i++)
            {
                Pawn baby = ___babies[i];
                if (baby == null || baby.Dead)
                {
                    continue;
                }

                firstLivingBaby ??= baby;
                Current.Game?.GetComponent<GameState>()?.RegisterPostpartumNewborn(mother, baby);
                BirthEffects.ApplyBabyBond(mother, baby);
            }
        }

        if (firstLivingBaby != null)
        {
            BirthEffects.ApplyFamilyEffects(mother, firstLivingBaby);
        }
    }
}
