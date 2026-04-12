using TeikeibunDanmaku.Blackboard;

namespace TeikeibunDanmaku.Core.Rules;

public sealed class AndCondition : ICondition
{
    private readonly IReadOnlyList<ICondition> _conditions;

    public AndCondition(IReadOnlyList<ICondition> conditions)
    {
        _conditions = conditions ?? throw new ArgumentNullException(nameof(conditions));
    }

    public bool Evaluate(IBoardState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (_conditions.Count == 0)
        {
            return false;
        }

        foreach (var condition in _conditions)
        {
            if (!condition.Evaluate(state))
            {
                return false;
            }
        }

        return true;
    }
}
