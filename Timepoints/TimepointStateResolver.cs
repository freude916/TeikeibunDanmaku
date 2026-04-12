using TeikeibunDanmaku.Blackboard;
using TeikeibunDanmaku.Core.Timepoints;

namespace TeikeibunDanmaku.Core.Rules;

public sealed class TimepointStateResolver : ITimepointStateResolver
{
    public Type ResolveStateType(string timepointId)
    {
        return timepointId switch
        {
            RewardSeenTimepoint.TimepointId => typeof(CardState),
            _ => throw new InvalidOperationException($"Unknown timepoint id '{timepointId}'.")
        };
    }
}
