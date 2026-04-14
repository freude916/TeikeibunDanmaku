using TeikeibunDanmaku.Core.Blackboard;

namespace TeikeibunDanmaku.Core.Condition;

public interface ICondition
{
    bool Evaluate(IBoardState state);
    ConditionDto Serialize();
}

public abstract class ConditionCodec
{
    public abstract string Type { get; }
    public abstract ICondition DeserializeDto(ConditionDto dto, Type stateType, ConditionDeserializer deserializer);
}
