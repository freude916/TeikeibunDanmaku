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

    public override LtCondition Deserialize(JsonElement json, Type stateType, ConditionDeserializer deserializer)
    {
        _ = deserializer ?? throw new ArgumentNullException(nameof(deserializer));
        ArgumentNullException.ThrowIfNull(stateType);

        var key = json.GetProperty("key").GetString()
                  ?? throw new JsonException("Property 'key' cannot be null.");
        var valueElement = json.GetProperty("value");
        var descriptors = BoardStateRegistry.GetFieldDescriptors(stateType);

        if (!descriptors.TryGetValue(key, out var descriptor))
        {
            throw new JsonException($"Condition key '{key}' was not found in state type '{stateType.Name}'.");
        }

        if (!TypeUtil.IsNumericType(descriptor.ValueType))
        {
            throw new JsonException($"Condition '{ConditionType.Lt}' requires numeric key '{key}'.");
        }

        if (!TypeUtil.TryParseNumericJsonAsDouble(valueElement, out var expectedValue))
        {
            throw new JsonException($"Value for key '{key}' must be numeric or numeric string.");
        }

        return new LtCondition(descriptor, expectedValue);
    }
}
