using TeikeibunDanmaku.Core.Blackboard;

namespace TeikeibunDanmaku.Timepoints;

public sealed class TimepointStateResolver
{
    private static readonly IReadOnlyList<TimepointDescriptor> Timepoints =
    [
        new(RewardSeenTimepoint.TimepointId, RewardSeenTimepoint.TimepointDisplayName, typeof(CardState)),
        new(ShopSeenTimepoint.TimepointId, ShopSeenTimepoint.TimepointDisplayName, typeof(CardState)),
        new(CardPlayedTimepoint.TimepointId, CardPlayedTimepoint.TimepointDisplayName, typeof(CardPlayState))
    ];

    public IReadOnlyList<TimepointDescriptor> ListTimepoints() => Timepoints;

    public string GetDisplayName(string timepointId)
    {
        var descriptor = Timepoints.FirstOrDefault(item => string.Equals(item.Id, timepointId, StringComparison.Ordinal));
        return string.IsNullOrWhiteSpace(descriptor.Id) ? timepointId : descriptor.DisplayName;
    }

    public Type ResolveStateType(string timepointId)
    {
        var descriptor = Timepoints.FirstOrDefault(item => string.Equals(item.Id, timepointId, StringComparison.Ordinal));
        return string.IsNullOrWhiteSpace(descriptor.Id)
            ? throw new InvalidOperationException($"Unknown timepoint id '{timepointId}'.")
            : descriptor.StateType;
    }
}
