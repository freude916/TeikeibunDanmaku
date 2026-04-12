using System.Text.Json;
using TeikeibunDanmaku.Blackboard;

namespace TeikeibunDanmaku.Core.Rules;

public static class ConditionDeserializer
{
    public static ICondition Deserialize(JsonElement json, Type stateType)
    {
        ArgumentNullException.ThrowIfNull(stateType);

        var type = json.GetProperty("type").GetString()
                   ?? throw new JsonException("Property 'type' cannot be null.");

        return type switch
        {
            ConditionType.Eq => DeserializeEq(json, stateType),
            ConditionType.And => DeserializeAnd(json, stateType),
            _ => throw new JsonException($"Unsupported condition type '{type}'.")
        };
    }

    private static ICondition DeserializeEq(JsonElement json, Type stateType)
    {
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

    private static ICondition DeserializeAnd(JsonElement json, Type stateType)
    {
        var conditionsElement = json.GetProperty("conditions");

        var conditions = conditionsElement
            .EnumerateArray()
            .Select(child => Deserialize(child, stateType))
            .ToArray();

        return new AndCondition(conditions);
    }

    private static object? ParseExpectedValue(JsonElement valueElement, Type targetType, string key)
    {
        var nonNullableType = Util.GetNonNullableType(targetType);
        if (Util.IsNumericType(nonNullableType))
        {
            if (Util.TryParseNumericJsonAsDouble(valueElement, out var parsedValue))
            {
                return parsedValue;
            }

            throw new JsonException($"Value for key '{key}' must be numeric or numeric string.");
        }

        return nonNullableType switch
        {
            _ when nonNullableType == typeof(string) => valueElement.GetString()
                 ?? throw new JsonException($"Value for key '{key}' must not be null."),
            _ when nonNullableType == typeof(bool) => Util.TryGetBooleanValue(valueElement, out var boolValue)
                 ? boolValue
                 : throw new JsonException($"Value for key '{key}' must be a boolean."),
            _ => JsonSerializer.Deserialize(valueElement.GetRawText(), nonNullableType)
                 ?? throw new JsonException($"Failed to deserialize value for key '{key}' as '{nonNullableType.Name}'.")
        };
    }

}
