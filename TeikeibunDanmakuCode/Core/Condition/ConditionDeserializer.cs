using System.Text.Json;

namespace TeikeibunDanmaku.Core.Condition;

public sealed class ConditionDeserializer
{
    private readonly ConditionRegistry _registry;

    public ConditionDeserializer(ConditionRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public ICondition DeserializeJson(JsonElement json, Type stateType)
    {
        var dto = JsonSerializer.Deserialize<ConditionDto>(json.GetRawText())
                  ?? throw new JsonException("Failed to deserialize condition DTO.");
        return DeserializeDto(dto, stateType);
    }

    public ICondition DeserializeDto(ConditionDto dto, Type stateType)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentNullException.ThrowIfNull(stateType);

        if (string.IsNullOrWhiteSpace(dto.Type))
        {
            throw new JsonException("Property 'type' cannot be null.");
        }

        return _registry.Resolve(dto.Type).DeserializeDto(dto, stateType, this);
    }
}
