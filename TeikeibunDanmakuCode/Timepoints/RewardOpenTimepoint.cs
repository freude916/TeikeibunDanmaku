using MegaCrit.Sts2.Core.Models;
using TeikeibunDanmaku.Core.Blackboard;

namespace TeikeibunDanmaku.Timepoints;

public sealed class RewardOpenTimepoint : Timepoint<CardPoolOpenState>
{
    public const string TimepointId = "reward.open";
    public const string TimepointDisplayName = "卡牌奖励打开";

    public override string Id => TimepointId;
    public override string DisplayName => TimepointDisplayName;

    public static RewardOpenTimepoint FromCards(IEnumerable<CardModel> cards)
    {
        ArgumentNullException.ThrowIfNull(cards);

        return new RewardOpenTimepoint
        {
            State = CardPoolOpenState.FromCards(cards)
        };
    }
}
