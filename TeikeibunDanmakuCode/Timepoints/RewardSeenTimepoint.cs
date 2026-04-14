using MegaCrit.Sts2.Core.Models;
using TeikeibunDanmaku.Core.Blackboard;

namespace TeikeibunDanmaku.Timepoints;

public sealed class RewardSeenTimepoint: Timepoint<CardState>
{
    public const string TimepointId = "reward.seen";

    public override string Id => TimepointId;

    public static RewardSeenTimepoint From(CardModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return new RewardSeenTimepoint
        {
            State = CardState.FromCardModel(model)
        };
    }
}
