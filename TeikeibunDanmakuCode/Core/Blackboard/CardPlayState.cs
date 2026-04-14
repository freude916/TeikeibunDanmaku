using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace TeikeibunDanmaku.Core.Blackboard;

public sealed class CardPlayState : IBoardState
{
    [DataField("卡牌ID")] public required string ModelId { get; init; }

    [DataField("卡牌名")] public required string Title { get; init; }

    [DataField("X费")] public required bool CostsX { get; init; }
    [DataField("能量消耗")] public required int EnergyCost { get; init; }

    [DataField("总伤害")] public required int DamageTotal { get; init; }
    [DataField("未格挡伤害")] public required int DamageUnblocked { get; init; }
    [DataField("已格挡伤害")] public required int DamageBlocked { get; init; }
    [DataField("伤害段数")] public required int DamageHits { get; init; }
    [DataField("单段最大伤害")] public required int DamageMaxSingle { get; init; }
    [DataField("对敌未格挡伤害")] public required int DamageEnemyUnblocked { get; init; }
    [DataField("是否造成敌方伤害")] public required bool HasEnemyDamage { get; init; }

    public static CardPlayState FromCardPlay(CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay);
        ArgumentNullException.ThrowIfNull(cardPlay.Card);

        var card = cardPlay.Card;
        var stats = ReadDamageStats(cardPlay);
        return new CardPlayState
        {
            ModelId = card.Id.Entry,
            Title = card.TitleLocString.GetFormattedText(),
            CostsX = card.EnergyCost.CostsX,
            EnergyCost = card.EnergyCost.GetAmountToSpend(),
            DamageTotal = stats.TotalDamage,
            DamageUnblocked = stats.UnblockedDamage,
            DamageBlocked = stats.BlockedDamage,
            DamageHits = stats.HitCount,
            DamageMaxSingle = stats.MaxSingleHitDamage,
            DamageEnemyUnblocked = stats.EnemyUnblockedDamage,
            HasEnemyDamage = stats.EnemyUnblockedDamage > 0,
        };
    }

    private static CardDamageStats ReadDamageStats(CardPlay cardPlay)
    {
        var history = CombatManager.Instance?.History;
        if (history == null)
            return default;

        var entries = history.Entries as IList<MegaCrit.Sts2.Core.Combat.History.CombatHistoryEntry> ?? [.. history.Entries];
        if (entries.Count == 0)
            return default;

        var startIndex = FindPlaySeriesStartIndex(entries, cardPlay);
        if (startIndex < 0)
            return default;

        var unblocked = 0;
        var blocked = 0;
        var total = 0;
        var hits = 0;
        var maxSingle = 0;
        var enemyUnblocked = 0;

        for (var i = startIndex; i < entries.Count; i++)
        {
            if (entries[i] is not DamageReceivedEntry damageEntry)
                continue;

            if (!IsSameCardSource(damageEntry.CardSource, cardPlay.Card))
                continue;

            var visibleUnblocked = damageEntry.Result.UnblockedDamage + damageEntry.Result.OverkillDamage;
            var visibleTotal = damageEntry.Result.TotalDamage + damageEntry.Result.OverkillDamage;

            hits++;
            unblocked += visibleUnblocked;
            blocked += damageEntry.Result.BlockedDamage;
            total += visibleTotal;
            maxSingle = Math.Max(maxSingle, visibleTotal);

            if (damageEntry.Receiver.IsEnemy)
                enemyUnblocked += visibleUnblocked;
        }

        return new CardDamageStats(unblocked, blocked, total, hits, maxSingle, enemyUnblocked);
    }

    private static int FindPlaySeriesStartIndex(IList<MegaCrit.Sts2.Core.Combat.History.CombatHistoryEntry> entries, CardPlay currentPlay)
    {
        var fallbackStart = -1;
        for (var i = entries.Count - 1; i >= 0; i--)
        {
            if (entries[i] is not CardPlayStartedEntry startEntry)
                continue;

            if (!IsSameCardSource(startEntry.CardPlay.Card, currentPlay.Card))
                continue;

            if (startEntry.CardPlay.PlayCount != currentPlay.PlayCount)
                continue;

            if (startEntry.CardPlay.IsFirstInSeries)
                return i;

            if (fallbackStart < 0)
                fallbackStart = i;
        }

        return fallbackStart;
    }

    private static bool IsSameCardSource(CardModel? historyCard, CardModel currentCard)
    {
        if (historyCard == null)
            return false;

        if (ReferenceEquals(historyCard, currentCard))
            return true;

        return string.Equals(historyCard.Id.Entry, currentCard.Id.Entry, StringComparison.Ordinal);
    }

    private readonly record struct CardDamageStats(
        int UnblockedDamage,
        int BlockedDamage,
        int TotalDamage,
        int HitCount,
        int MaxSingleHitDamage,
        int EnemyUnblockedDamage
    );
}
