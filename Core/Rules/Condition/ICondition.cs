using TeikeibunDanmaku.Blackboard;

namespace TeikeibunDanmaku.Core.Rules;

public interface ICondition
{
    bool Evaluate(IBoardState state);
}
