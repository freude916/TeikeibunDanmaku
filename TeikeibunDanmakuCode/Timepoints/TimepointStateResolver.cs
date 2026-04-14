using TeikeibunDanmaku.Core.Blackboard;

namespace TeikeibunDanmaku.Timepoints;

public sealed class TimepointStateResolver
{
    public Type ResolveStateType(string timepointId)
    {
        return timepointId switch
        {
            RewardSeenTimepoint.TimepointId => typeof(CardState),
            ShopSeenTimepoint.TimepointId => typeof(CardState),
            _ => throw new InvalidOperationException($"Unknown timepoint id '{timepointId}'.")
        };
    }
}
