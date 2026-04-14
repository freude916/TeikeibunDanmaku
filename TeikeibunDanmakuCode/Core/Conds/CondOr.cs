using System.Text.Json;
using TeikeibunDanmaku.Core.Blackboard;

namespace TeikeibunDanmaku.Core.Condition;

public sealed class CondOr : ICondition
{
    private readonly IReadOnlyList<ICondition> _conditions;
    public IReadOnlyList<ICondition> Conditions => _conditions;

    public CondOr(IReadOnlyList<ICondition> conditions)
    {
        _conditions = conditions ?? throw new ArgumentNullException(nameof(conditions));
    }

    public bool Evaluate(IBoardState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        return _conditions.Count != 0 && _conditions.Any(condition => condition.Evaluate(state));
    }

    public ConditionDto Serialize()
    {
        return new ConditionDto
        {
            Type = ConditionType.CondOr,
            Conditions = _conditions.Select(condition => condition.Serialize())
        };
    }
}

public sealed class OrConditionCodec : ConditionCodec
{
    public override string Type => ConditionType.CondOr;

    public override CondOr DeserializeDto(ConditionDto dto, Type stateType, ConditionDeserializer deserializer)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentNullException.ThrowIfNull(stateType);
        ArgumentNullException.ThrowIfNull(deserializer);

        var conditions = (dto.Conditions ?? throw new JsonException("Property 'conditions' cannot be null."))
            .Select(child => deserializer.DeserializeDto(child, stateType))
            .ToArray();

        return new CondOr(conditions);
    }
}
