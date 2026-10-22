using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace FertilityExpanded;

public static class FosterReturnReward
{
    private const int RewardChoiceCount = 3;
    private const float RewardValue = 1000f;
    private const float MinimumRewardValue = 450f;

    public static void TryCreateRewardQuest(Pawn child, Faction sourceFaction, Map rewardMap)
    {
        if (child == null || rewardMap?.Parent == null || FE_DefOf.FE_FosterReturnReward == null)
        {
            return;
        }

        Quest quest = Quest.MakeRaw();
        quest.root = FE_DefOf.FE_FosterReturnReward;
        quest.name = "FE_FosterReturnRewardQuestName".Translate(child.Named("CHILD"));
        quest.description = "FE_FosterReturnRewardQuestDescription".Translate(child.Named("CHILD"), SourceFactionName(sourceFaction).Named("FACTION"));
        quest.points = StorytellerUtility.DefaultThreatPointsNow(rewardMap);

        QuestPart_Choice rewardChoice = new QuestPart_Choice
        {
            inSignalChoiceUsed = quest.InitiateSignal
        };
        quest.AddPart(rewardChoice);

        for (int i = 0; i < RewardChoiceCount; i++)
        {
            QuestPart_Choice.Choice choice = MakeRewardChoice(sourceFaction, rewardMap, quest.InitiateSignal);
            if (!choice.rewards.NullOrEmpty() && !choice.questParts.NullOrEmpty())
            {
                rewardChoice.choices.Add(choice);
                for (int j = 0; j < choice.questParts.Count; j++)
                {
                    quest.AddPart(choice.questParts[j]);
                }
            }
        }

        if (rewardChoice.choices.NullOrEmpty())
        {
            return;
        }

        QuestPart_QuestEnd questEnd = new QuestPart_QuestEnd
        {
            inSignal = quest.InitiateSignal,
            outcome = QuestEndOutcome.Success,
            sendLetter = false,
            playSound = false
        };
        quest.AddPart(questEnd);

        Find.QuestManager.Add(quest);
        TaggedString label = "FE_FosterReturnRewardLetterLabel".Translate(child.Named("CHILD"));
        TaggedString text = "FE_FosterReturnRewardLetterText".Translate(child.Named("CHILD"), SourceFactionName(sourceFaction).Named("FACTION"));
        Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.PositiveEvent, rewardMap.Parent, sourceFaction, quest);
    }

    private static QuestPart_Choice.Choice MakeRewardChoice(Faction sourceFaction, Map rewardMap, string inSignal)
    {
        RewardsGeneratorParams parms = new RewardsGeneratorParams
        {
            rewardValue = RewardValue,
            minGeneratedRewardValue = MinimumRewardValue,
            giverFaction = sourceFaction,
            thingRewardRequired = true,
            thingRewardItemsOnly = true,
            allowGoodwill = false,
            allowRoyalFavor = false
        };

        QuestPart_Choice.Choice choice = new QuestPart_Choice.Choice();
        List<Reward> rewards = RewardsGenerator.Generate(parms);
        for (int i = 0; i < rewards.Count; i++)
        {
            choice.rewards.Add(rewards[i]);
            if (rewards[i] is Reward_Items rewardItems && !rewardItems.items.NullOrEmpty())
            {
                QuestPart_DropPods dropPods = new QuestPart_DropPods
                {
                    inSignal = inSignal,
                    mapParent = rewardMap.Parent,
                    useTradeDropSpot = true,
                    canRetargetAnyMap = true,
                    customLetterLabel = "FE_FosterReturnRewardArrivedLetterLabel".Translate(),
                    customLetterText = "FE_FosterReturnRewardArrivedLetterText".Translate(),
                    customLetterDef = LetterDefOf.PositiveEvent
                };
                dropPods.Things = rewardItems.items;
                choice.questParts.Add(dropPods);
            }
        }

        return choice;
    }

    private static string SourceFactionName(Faction sourceFaction)
    {
        return sourceFaction != null && !sourceFaction.Name.NullOrEmpty()
            ? sourceFaction.Name
            : "FE_DistantSettlement".Translate();
    }
}
