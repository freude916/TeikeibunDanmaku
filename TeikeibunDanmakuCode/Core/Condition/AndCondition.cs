using System.Text.Json;
using TeikeibunDanmaku.Core.Blackboard;

namespace TeikeibunDanmaku.Core.Condition;

public sealed class AndCondition : ICondition
{
    private readonly IReadOnlyList<ICondition> _conditions;
    public IReadOnlyList<ICondition> Conditions => _conditions;

    public AndCondition(IReadOnlyList<ICondition> conditions)
    {
        _conditions = conditions ?? throw new ArgumentNullException(nameof(conditions));
    }

    public bool Evaluate(IBoardState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        return _conditions.Count != 0 && _conditions.All(condition => condition.Evaluate(state));
    }

    public ConditionDto Serialize()
    {
        return new ConditionDto
        {
            Type = ConditionType.And,
            Conditions = _conditions.Select(condition => condition.Serialize())
        };
    }
}

public sealed class AndConditionCodec : ConditionCodec
{
    public override string Type => ConditionType.And;

    public override AndCondition DeserializeDto(ConditionDto dto, Type stateType, ConditionDeserializer deserializer)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentNullException.ThrowIfNull(stateType);
        ArgumentNullException.ThrowIfNull(deserializer);

        var conditions = (dto.Conditions ?? throw new JsonException("Property 'conditions' cannot be null."))
            .Select(child => deserializer.DeserializeDto(child, stateType))
            .ToArray();

        return new AndCondition(conditions);
    }
}
