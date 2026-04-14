using System.Text.Json;
using TeikeibunDanmaku.Core.Blackboard;

namespace TeikeibunDanmaku.Core.Condition;

public sealed class FindCondition(BoardFieldDescriptor fieldDescriptor, string needle)
    : ICondition
{
    private readonly BoardFieldDescriptor _fieldDescriptor = fieldDescriptor ?? throw new ArgumentNullException(nameof(fieldDescriptor));
    private readonly string _needle = needle ?? throw new ArgumentNullException(nameof(needle));

    public BoardFieldDescriptor FieldDescriptor => _fieldDescriptor;
    public string Needle => _needle;

    public bool Evaluate(IBoardState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var actualValue = _fieldDescriptor.Getter(state) as string;
        return actualValue is not null && actualValue.Contains(_needle, StringComparison.Ordinal);
    }

    public ConditionDto Serialize()
    {
        return new ConditionDto
        {
            Type = ConditionType.Find,
            Key = FieldDescriptor.Name,
            Value = Needle
        };
    }
}

public sealed class FindConditionCodec : ConditionCodec
{
    public override string Type => ConditionType.Find;

    public override FindCondition DeserializeDto(ConditionDto dto, Type stateType, ConditionDeserializer deserializer)
    {
        ArgumentNullException.ThrowIfNull(dto);
        _ = deserializer ?? throw new ArgumentNullException(nameof(deserializer));
        ArgumentNullException.ThrowIfNull(stateType);

        var key = dto.Key ?? throw new JsonException("Property 'key' cannot be null.");
        var value = GetStringValue(dto.Value, key);
        var descriptors = BoardStateRegistry.GetFieldDescriptors(stateType);

        if (!descriptors.TryGetValue(key, out var descriptor))
        {
            throw new JsonException($"Condition key '{key}' was not found in state type '{stateType.Name}'.");
        }

        if (descriptor.ValueType != typeof(string))
        {
            throw new JsonException($"Condition '{ConditionType.Find}' requires string key '{key}'.");
        }

        return new FindCondition(descriptor, value);
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
