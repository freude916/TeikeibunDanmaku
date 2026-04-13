using System.Text.Json;
using TeikeibunDanmaku.Core.Blackboard;
using TeikeibunDanmaku.Utils;

namespace TeikeibunDanmaku.Core.Condition;

public sealed class EqCondition(BoardFieldDescriptor fieldDescriptor, object? expectedValue)
    : ICondition
{
    private const double Epsilon = 1e-9;

    private readonly BoardFieldDescriptor _fieldDescriptor = fieldDescriptor ?? throw new ArgumentNullException(nameof(fieldDescriptor));
    public BoardFieldDescriptor FieldDescriptor => _fieldDescriptor;
    public object? ExpectedValue => expectedValue;

    public bool Evaluate(IBoardState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var actualValue = _fieldDescriptor.Getter(state);
        if (!TypeUtil.IsNumericType(_fieldDescriptor.ValueType)) return Equals(actualValue, ExpectedValue);
        
        if (!TypeUtil.TryConvertNumericObjectToDouble(actualValue, out var actualNumber) ||
            !TypeUtil.TryConvertNumericObjectToDouble(ExpectedValue, out var expectedNumber))
        {
            return false;
        }

        return Math.Abs(actualNumber - expectedNumber) <= Epsilon;

    }

    public ConditionDto Serialize()
    {
        return new ConditionDto
        {
            Type = ConditionType.Eq,
            Key = FieldDescriptor.Name,
            Value = ExpectedValue
        };
    }
}

public sealed class EqConditionCodec : ConditionCodec
{
    public override string Type => ConditionType.Eq;

    public override EqCondition Deserialize(JsonElement json, Type stateType, ConditionDeserializer deserializer)
    {
        ArgumentNullException.ThrowIfNull(stateType);
        ArgumentNullException.ThrowIfNull(deserializer);

        var key = json.GetProperty("key").GetString()
                  ?? throw new JsonException("Property 'key' cannot be null.");
        var valueElement = json.GetProperty("value");
        var descriptors = BoardStateRegistry.GetFieldDescriptors(stateType);

        if (!descriptors.TryGetValue(key, out var descriptor))
        {
            throw new JsonException($"Condition key '{key}' was not found in state type '{stateType.Name}'.");
        }

        var expectedValue = ParseExpectedValue(valueElement, descriptor.ValueType, key);
        return new EqCondition(descriptor, expectedValue);
    }

    private static object? ParseExpectedValue(JsonElement valueElement, Type targetType, string key)
    {
        var nonNullableType = TypeUtil.GetNonNullableType(targetType);
        if (TypeUtil.IsNumericType(nonNullableType))
        {
            if (TypeUtil.TryParseNumericJsonAsDouble(valueElement, out var parsedValue))
            {
                return parsedValue;
            }

            throw new JsonException($"Value for key '{key}' must be numeric or numeric string.");
        }

        return nonNullableType switch
        {
            _ when nonNullableType == typeof(string) => valueElement.GetString()
                 ?? throw new JsonException($"Value for key '{key}' must not be null."),
            _ when nonNullableType == typeof(bool) => TypeUtil.TryGetBooleanValue(valueElement, out var boolValue)
                 ? boolValue
                 : throw new JsonException($"Value for key '{key}' must be a boolean."),
            _ => JsonSerializer.Deserialize(valueElement.GetRawText(), nonNullableType)
                 ?? throw new JsonException($"Failed to deserialize value for key '{key}' as '{nonNullableType.Name}'.")
        };
    }
}
