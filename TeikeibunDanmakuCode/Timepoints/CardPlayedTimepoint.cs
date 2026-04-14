using MegaCrit.Sts2.Core.Entities.Cards;
using TeikeibunDanmaku.Core.Blackboard;

namespace TeikeibunDanmaku.Timepoints;

public sealed class CardPlayedTimepoint : Timepoint<CardPlayState>
{
    public const string TimepointId = "combat.card_played";
    public const string TimepointDisplayName = "战斗出牌完成";

    public override string Id => TimepointId;
    public override string DisplayName => TimepointDisplayName;

    public static CardPlayedTimepoint From(CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay);

        return new CardPlayedTimepoint
        {
            State = CardPlayState.FromCardPlay(cardPlay)
        };
    }
}
