using System.Text.Json;
using TeikeibunDanmaku.Core.Blackboard;
using TeikeibunDanmaku.Utils;

namespace TeikeibunDanmaku.Core.Condition;

public sealed class LtCondition(BoardFieldDescriptor fieldDescriptor, double expectedValue)
    : ICondition
{
    private readonly BoardFieldDescriptor _fieldDescriptor = fieldDescriptor ?? throw new ArgumentNullException(nameof(fieldDescriptor));

    public BoardFieldDescriptor FieldDescriptor => _fieldDescriptor;
    public double ExpectedValue => expectedValue;

    public bool Evaluate(IBoardState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var actualValue = _fieldDescriptor.Getter(state);
        if (!TypeUtil.TryConvertNumericObjectToDouble(actualValue, out var actualNumber))
        {
            return false;
        }

        return actualNumber < ExpectedValue;
    }

    public ConditionDto Serialize()
    {
        return new ConditionDto
        {
            Type = ConditionType.Lt,
            Key = FieldDescriptor.Name,
            Value = ExpectedValue
        };
    }
}

public sealed class LtConditionCodec : ConditionCodec
{
    public override string Type => ConditionType.Lt;

    public override LtCondition DeserializeDto(ConditionDto dto, Type stateType, ConditionDeserializer deserializer)
    {
        ArgumentNullException.ThrowIfNull(dto);
        _ = deserializer ?? throw new ArgumentNullException(nameof(deserializer));
        ArgumentNullException.ThrowIfNull(stateType);

        var key = dto.Key ?? throw new JsonException("Property 'key' cannot be null.");
        var value = dto.Value ?? throw new JsonException("Property 'value' cannot be null.");
        var descriptors = BoardStateRegistry.GetFieldDescriptors(stateType);

        if (!descriptors.TryGetValue(key, out var descriptor))
        {
            throw new JsonException($"Condition key '{key}' was not found in state type '{stateType.Name}'.");
        }

        if (!TypeUtil.IsNumericType(descriptor.ValueType))
        {
            throw new JsonException($"Condition '{ConditionType.Lt}' requires numeric key '{key}'.");
        }

        if (!TryParseNumericAsDouble(value, out var expectedValue))
        {
            throw new JsonException($"Value for key '{key}' must be numeric or numeric string.");
        }

        return new LtCondition(descriptor, expectedValue);
    }

    private static bool TryParseNumericAsDouble(object value, out double result)
    {
        if (value is JsonElement element)
        {
            return TypeUtil.TryParseNumericJsonAsDouble(element, out result);
        }

        return TypeUtil.TryConvertNumericObjectToDouble(value, out result);
    }
}
