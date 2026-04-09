using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace TeikeibunDanmaku.Core;

public static class CardPlayDanmakuEngine
{
    public static void OnCardPlayed(CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay);
        ArgumentNullException.ThrowIfNull(cardPlay.Card);

        var card = cardPlay.Card;
        var runtimeTags = BuildRuntimeTags(card);
        var matchedRule = FindFirstMatchedRule(runtimeTags);
        var text = BuildDanmakuText(card, matchedRule);

        DanmakuEventBus.Publish(text);

        MainFile.Logger.Debug(
            $"CardPlayDanmaku: card={card.Id.Entry}, type={card.Type}, tags=[{string.Join(",", runtimeTags)}], rule={(matchedRule?.RuleId ?? "fallback")}"
        );
    }

    private static CardDanmakuRule? FindFirstMatchedRule(HashSet<string> runtimeTags)
    {
        foreach (var rule in CardDanmakuRuleStore.GetCombatCardPlayedRules())
        {
            if (!HasAllRequiredTags(runtimeTags, rule.RequiredTags))
                continue;

            return rule;
        }

        return null;
    }

    private static bool HasAllRequiredTags(HashSet<string> runtimeTags, IReadOnlyList<string> requiredTags)
    {
        foreach (var requiredTag in requiredTags)
        {
            if (!runtimeTags.Contains(requiredTag))
                return false;
        }

        return true;
    }

    private static string BuildDanmakuText(CardModel card, CardDanmakuRule? matchedRule)
    {
        if (matchedRule == null || matchedRule.Messages.Count == 0)
            return $"打出 {card.Title}";

        var selectedMessage = matchedRule.Messages[Random.Shared.Next(matchedRule.Messages.Count)];
        return $"{selectedMessage}（{card.Title}）";
    }

    private static HashSet<string> BuildRuntimeTags(CardModel card)
    {
        var runtimeTags = new HashSet<string>(StringComparer.Ordinal)
        {
            "timepoint.combat.card_played",
            $"card.type.{ToTagToken(card.Type.ToString())}",
            $"card.id.{ToTagToken(card.Id.Entry)}"
        };

        foreach (var cardTag in card.Tags)
        {
            if (cardTag == CardTag.None)
                continue;

            runtimeTags.Add($"card.tag.{ToTagToken(cardTag.ToString())}");
        }

        return runtimeTags;
    }

    private static string ToTagToken(string value)
    {
        return value.Trim().ToLowerInvariant().Replace('-', '_').Replace(' ', '_');
    }
}
