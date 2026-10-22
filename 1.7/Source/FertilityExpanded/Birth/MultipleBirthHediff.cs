using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace FertilityExpanded;

public class MultipleBirthComp : HediffComp
{
    private const float SingleBirthWeight = 97.5f;
    private const float TwinBirthWeight = 2f;
    private const float TripletBirthWeight = 0.5f;

    private int babyCount = 1;
    private bool rolled;
    private bool extraBirthsConsumed;

    public int BabyCount => babyCount < 1 ? 1 : babyCount;

    public override void CompPostPostAdd(DamageInfo? dinfo)
    {
        base.CompPostPostAdd(dinfo);

        if (parent.def == HediffDefOf.PregnantHuman)
        {
            EnsureRolled();
        }
    }

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Values.Look(ref babyCount, "babyCount", 1);
        Scribe_Values.Look(ref rolled, "rolled", false);
        Scribe_Values.Look(ref extraBirthsConsumed, "extraBirthsConsumed", false);

        if (Scribe.mode == LoadSaveMode.PostLoadInit && (babyCount < 1 || babyCount > 3))
        {
            babyCount = 1;
        }
    }

    public override IEnumerable<Gizmo> CompGetGizmos()
    {
        IEnumerable<Gizmo> baseGizmos = base.CompGetGizmos();
        if (baseGizmos != null)
        {
            foreach (Gizmo gizmo in baseGizmos)
            {
                yield return gizmo;
            }
        }

        if (!DebugSettings.ShowDevGizmos || Pawn == null)
        {
            yield break;
        }

        yield return ForceBabyCountGizmo(1, "FE_DevForceSingle".Translate());
        yield return ForceBabyCountGizmo(2, "FE_DevForceTwins".Translate());
        yield return ForceBabyCountGizmo(3, "FE_DevForceTriplets".Translate());
    }

    public void EnsureRolled()
    {
        if (rolled)
        {
            return;
        }

        rolled = true;
        babyCount = RollBabyCount();
    }

    public void CopyFrom(MultipleBirthComp source)
    {
        if (source == null)
        {
            return;
        }

        source.EnsureRolled();
        rolled = true;
        babyCount = source.BabyCount;
        extraBirthsConsumed = false;
    }

    public void SetBabyCountForDebug(int count)
    {
        if (count < 1)
        {
            count = 1;
        }
        else if (count > 3)
        {
            count = 3;
        }

        babyCount = count;
        rolled = true;
        extraBirthsConsumed = false;
    }

    public int ConsumeExtraBirths()
    {
        EnsureRolled();
        if (extraBirthsConsumed)
        {
            return 0;
        }

        extraBirthsConsumed = true;
        return BabyCount - 1;
    }

    private int RollBabyCount()
    {
        bool hasMultipleBirthGene = HasMultipleBirthGene();
        return Features.MultibirthRebalance
            ? RollRebalancedBabyCount(hasMultipleBirthGene)
            : RollVanillaBabyCount(hasMultipleBirthGene);
    }

    private static int RollRebalancedBabyCount(bool hasMultipleBirthGene)
    {
        float multipleFactor = hasMultipleBirthGene
            ? MultipleBirthGene.MultipleBirthChanceFactor
            : 1f;
        float twinWeight = TwinBirthWeight * multipleFactor;
        float tripletWeight = TripletBirthWeight * multipleFactor;
        float roll = Rand.Range(0f, SingleBirthWeight + twinWeight + tripletWeight);

        if (roll < SingleBirthWeight)
        {
            return 1;
        }

        roll -= SingleBirthWeight;
        return roll < twinWeight ? 2 : 3;
    }

    private int RollVanillaBabyCount(bool hasMultipleBirthGene)
    {
        SimpleCurve litterSizeCurve = Pawn?.RaceProps?.litterSizeCurve
            ?? ThingDefOf.Human?.race?.litterSizeCurve;
        if (litterSizeCurve == null)
        {
            return 1;
        }

        float maximumOutcomeWeight = hasMultipleBirthGene
            ? MultipleBirthGene.MultipleBirthChanceFactor
            : 1f;
        while (true)
        {
            int count = Mathf.Clamp(Mathf.RoundToInt(Rand.ByCurve(litterSizeCurve)), 1, 3);
            float outcomeWeight = hasMultipleBirthGene && count > 1
                ? MultipleBirthGene.MultipleBirthChanceFactor
                : 1f;
            if (outcomeWeight >= maximumOutcomeWeight
                || Rand.Chance(outcomeWeight / maximumOutcomeWeight))
            {
                return count;
            }
        }
    }

    private bool HasMultipleBirthGene()
    {
        if (parent is not HediffWithParents pregnancy)
        {
            return HasActiveMultipleBirthGene(Pawn);
        }

        return HasActiveMultipleBirthGene(pregnancy.pawn)
            || HasActiveMultipleBirthGene(pregnancy.Mother);
    }

    private static bool HasActiveMultipleBirthGene(Pawn pawn)
    {
        return pawn?.genes?.HasActiveGene(FE_DefOf.FE_MultipleBirthTendency) == true;
    }

    private Command_Action ForceBabyCountGizmo(int count, string label)
    {
        return new Command_Action
        {
            defaultLabel = label,
            action = delegate
            {
                SetBabyCountForDebug(count);
                Messages.Message("FE_DevBirthCount".Translate(Pawn.Named("PAWN"), count.Named("COUNT")), Pawn, MessageTypeDefOf.NeutralEvent, historical: false);
            }
        };
    }
}

public class MultipleBirthProperties : HediffCompProperties
{
    public MultipleBirthProperties()
    {
        compClass = typeof(MultipleBirthComp);
    }
}
