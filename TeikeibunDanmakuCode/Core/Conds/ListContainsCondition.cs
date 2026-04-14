using System.Collections;
using System.Text.Json;
using TeikeibunDanmaku.Core.Blackboard;
using TeikeibunDanmaku.Utils;

namespace TeikeibunDanmaku.Core.Condition;

public sealed class ListContainsCondition(BoardFieldDescriptor fieldDescriptor, string expected)
    : ICondition
{
    private readonly BoardFieldDescriptor _fieldDescriptor = fieldDescriptor ?? throw new ArgumentNullException(nameof(fieldDescriptor));
    private readonly string _expected = expected ?? throw new ArgumentNullException(nameof(expected));

    public BoardFieldDescriptor FieldDescriptor => _fieldDescriptor;
    public string Expected => _expected;

    public bool Evaluate(IBoardState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var value = _fieldDescriptor.Getter(state);
        if (value is IEnumerable<string> stringEnumerable)
        {
            return stringEnumerable.Any(item => string.Equals(item, _expected, StringComparison.Ordinal));
        }

        if (value is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                if (item is string text && string.Equals(text, _expected, StringComparison.Ordinal))
                    return true;
            }
        }

        return false;
    }

    public ConditionDto Serialize()
    {
        return new ConditionDto
        {
            Type = ConditionType.ListContains,
            Key = FieldDescriptor.Name,
            Value = Expected
        };
    }
}

public sealed class ListContainsConditionCodec : ConditionCodec
{
    public override string Type => ConditionType.ListContains;

    public override ListContainsCondition DeserializeDto(ConditionDto dto, Type stateType, ConditionDeserializer deserializer)
    {
        ArgumentNullException.ThrowIfNull(dto);
        _ = deserializer ?? throw new ArgumentNullException(nameof(deserializer));
        ArgumentNullException.ThrowIfNull(stateType);

        var key = dto.Key ?? throw new JsonException("Property 'key' cannot be null.");
        var value = GetStringValue(dto.Value, key);
        var descriptors = FieldDescriptorResolver.GetFieldDescriptors(stateType);

        if (!descriptors.TryGetValue(key, out var descriptor))
            throw new JsonException($"Condition key '{key}' was not found in state type '{stateType.Name}'.");

        if (!TypeUtil.IsStringEnumerableType(descriptor.ValueType))
            throw new JsonException($"Condition '{ConditionType.ListContains}' requires string-list key '{key}'.");

        return new ListContainsCondition(descriptor, value);
    }

    private static string GetStringValue(object? value, string key)
    {
        return value switch
        {
            string stringValue => stringValue,
            JsonElement { ValueKind: JsonValueKind.String } element => element.GetString()
                ?? throw new JsonException($"Value for key '{key}' must be a string."),
            _ => throw new JsonException($"Value for key '{key}' must be a string.")
        };
    }
}
