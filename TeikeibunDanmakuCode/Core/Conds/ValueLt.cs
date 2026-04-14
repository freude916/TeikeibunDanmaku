using System.Collections;
using System.Text.Json;
using TeikeibunDanmaku.Core.Blackboard;
using TeikeibunDanmaku.Utils;

namespace TeikeibunDanmaku.Core.Condition;

public sealed class ValueLt(BoardFieldDescriptor fieldDescriptor, double expectedValue)
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
            Type = ConditionType.ValueLt,
            Key = FieldDescriptor.Name,
            Value = ExpectedValue
        };
    }
}

public sealed class ValueLtListCount(BoardFieldDescriptor fieldDescriptor, string item, double expectedCount)
    : ICondition
{
    private readonly BoardFieldDescriptor _fieldDescriptor = fieldDescriptor ?? throw new ArgumentNullException(nameof(fieldDescriptor));
    private readonly string _item = item ?? throw new ArgumentNullException(nameof(item));

    public BoardFieldDescriptor FieldDescriptor => _fieldDescriptor;
    public string Item => _item;
    public double ExpectedCount => expectedCount;

    public bool Evaluate(IBoardState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var actualCount = CountMatches(_fieldDescriptor.Getter(state), _item);
        return actualCount < ExpectedCount;
    }

    public ConditionDto Serialize()
    {
        return new ConditionDto
        {
            Type = ConditionType.ValueLt,
            Key = FieldDescriptor.Name,
            Value = new Dictionary<string, object?>
            {
                ["item"] = Item,
                ["count"] = ExpectedCount
            }
        };
    }

    private static int CountMatches(object? value, string item)
    {
        if (value is IEnumerable<string> stringEnumerable)
            return stringEnumerable.Count(entry => string.Equals(entry, item, StringComparison.Ordinal));

        if (value is IEnumerable enumerable)
        {
            var count = 0;
            foreach (var entry in enumerable)
            {
                if (entry is string text && string.Equals(text, item, StringComparison.Ordinal))
                    count++;
            }

            return count;
        }

        return 0;
    }
}

public sealed class LtConditionCodec : ConditionCodec
{
    public override string Type => ConditionType.ValueLt;

    public override ICondition DeserializeDto(ConditionDto dto, Type stateType, ConditionDeserializer deserializer)
    {
        ArgumentNullException.ThrowIfNull(dto);
        _ = deserializer ?? throw new ArgumentNullException(nameof(deserializer));
        ArgumentNullException.ThrowIfNull(stateType);

        var key = dto.Key ?? throw new JsonException("Property 'key' cannot be null.");
        var value = dto.Value ?? throw new JsonException("Property 'value' cannot be null.");
        var descriptors = FieldDescriptorResolver.GetFieldDescriptors(stateType);

        if (!descriptors.TryGetValue(key, out var descriptor))
        {
            throw new JsonException($"Condition key '{key}' was not found in state type '{stateType.Name}'.");
        }

        if (TypeUtil.IsNumericType(descriptor.ValueType))
        {
            if (!TryParseNumericAsDouble(value, out var expectedValue))
            {
                throw new JsonException($"Value for key '{key}' must be numeric or numeric string.");
            }

            return new ValueLt(descriptor, expectedValue);
        }

        if (!TypeUtil.IsStringEnumerableType(descriptor.ValueType))
        {
            throw new JsonException($"Condition '{ConditionType.ValueLt}' requires numeric key or string-list key '{key}'.");
        }

        var (item, count) = ParseListCountValue(value, key);
        return new ValueLtListCount(descriptor, item, count);
    }

    private static bool TryParseNumericAsDouble(object value, out double result)
    {
        if (value is JsonElement element)
        {
            return TypeUtil.TryParseNumericJsonAsDouble(element, out result);
        }

        return TypeUtil.TryConvertNumericObjectToDouble(value, out result);
    }

    private static (string Item, double Count) ParseListCountValue(object value, string key)
    {
        if (value is JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Object)
                throw new JsonException($"Value for key '{key}' must be an object with properties 'item' and 'count'.");

            var item = element.TryGetProperty("item", out var itemElement) && itemElement.ValueKind == JsonValueKind.String
                ? itemElement.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(item))
                throw new JsonException($"Value for key '{key}' requires non-empty string property 'item'.");

            if (!element.TryGetProperty("count", out var countElement) ||
                !TypeUtil.TryParseNumericJsonAsDouble(countElement, out var countFromJson))
            {
                throw new JsonException($"Value for key '{key}' requires numeric property 'count'.");
            }

            return (item, countFromJson);
        }

        if (value is IDictionary<string, object?> dictionary)
        {
            if (!dictionary.TryGetValue("item", out var rawItem) || rawItem is not string item || string.IsNullOrWhiteSpace(item))
                throw new JsonException($"Value for key '{key}' requires non-empty string property 'item'.");

            if (!dictionary.TryGetValue("count", out var rawCount) || !TypeUtil.TryConvertNumericObjectToDouble(rawCount, out var count))
                throw new JsonException($"Value for key '{key}' requires numeric property 'count'.");

            return (item, count);
        }

        throw new JsonException($"Value for key '{key}' must be an object with properties 'item' and 'count'.");
    }
}
