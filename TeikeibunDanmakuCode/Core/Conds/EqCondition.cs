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

    public override EqCondition DeserializeDto(ConditionDto dto, Type stateType, ConditionDeserializer deserializer)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentNullException.ThrowIfNull(stateType);
        ArgumentNullException.ThrowIfNull(deserializer);

        var key = dto.Key ?? throw new JsonException("Property 'key' cannot be null.");
        var value = dto.Value;
        var descriptors = BoardStateRegistry.GetFieldDescriptors(stateType);

        if (!descriptors.TryGetValue(key, out var descriptor))
        {
            throw new JsonException($"Condition key '{key}' was not found in state type '{stateType.Name}'.");
        }

        var expectedValue = ParseExpectedValue(value, descriptor.ValueType, key);
        return new EqCondition(descriptor, expectedValue);
    }

    private static object? ParseExpectedValue(object? value, Type targetType, string key)
    {
        var nonNullableType = TypeUtil.GetNonNullableType(targetType);
        if (TypeUtil.IsNumericType(nonNullableType))
        {
            if (TryParseNumericAsDouble(value, out var parsedValue))
            {
                return parsedValue;
            }

            throw new JsonException($"Value for key '{key}' must be numeric or numeric string.");
        }

        return nonNullableType switch
        {
            _ when nonNullableType == typeof(string) => ParseStringValue(value, key),
            _ when nonNullableType == typeof(bool) => ParseBooleanValue(value, key),
            _ => ParseComplexValue(value, nonNullableType, key)
        };
    }

    private static bool TryParseNumericAsDouble(object? value, out double result)
    {
        if (value is JsonElement element)
        {
            return TypeUtil.TryParseNumericJsonAsDouble(element, out result);
        }

        return TypeUtil.TryConvertNumericObjectToDouble(value, out result);
    }

    private static string ParseStringValue(object? value, string key)
    {
        return value switch
        {
            string stringValue => stringValue,
            JsonElement { ValueKind: JsonValueKind.String } element => element.GetString()
                ?? throw new JsonException($"Value for key '{key}' must not be null."),
            _ => throw new JsonException($"Value for key '{key}' must be a string.")
        };
    }

    private static bool ParseBooleanValue(object? value, string key)
    {
        if (value is JsonElement element)
        {
            return TypeUtil.TryGetBooleanValue(element, out var boolValue)
                ? boolValue
                : throw new JsonException($"Value for key '{key}' must be a boolean.");
        }

        if (value is bool boolValueDirect)
        {
            return boolValueDirect;
        }

        throw new JsonException($"Value for key '{key}' must be a boolean.");
    }

    private static object ParseComplexValue(object? value, Type nonNullableType, string key)
    {
        if (value is null)
        {
            throw new JsonException($"Failed to deserialize value for key '{key}' as '{nonNullableType.Name}'.");
        }

        if (value is JsonElement element)
        {
            return JsonSerializer.Deserialize(element.GetRawText(), nonNullableType)
                   ?? throw new JsonException($"Failed to deserialize value for key '{key}' as '{nonNullableType.Name}'.");
        }

        try
        {
            var serialized = JsonSerializer.Serialize(value);
            return JsonSerializer.Deserialize(serialized, nonNullableType)
                   ?? throw new JsonException($"Failed to deserialize value for key '{key}' as '{nonNullableType.Name}'.");
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new JsonException($"Failed to deserialize value for key '{key}' as '{nonNullableType.Name}'.", ex);
        }
    }
}
