using TeikeibunDanmaku.Timepoints;

namespace TeikeibunDanmaku.Core.Rules;

public sealed class Rule
{
    public required string RuleId { get; init; }
    public required string TimepointId { get; init; }
    public required ICondition Condition { get; init; }
    public required IReadOnlyList<string> Messages { get; init; }

    public bool Matches(Timepoint timepoint)
    {
        ArgumentNullException.ThrowIfNull(timepoint);

        return timepoint.Id == TimepointId && Condition.Evaluate(timepoint.BoardState);
    }
}
