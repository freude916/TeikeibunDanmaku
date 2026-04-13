using System.Text.Json;

namespace TeikeibunDanmaku.Core.Condition;

public sealed class ConditionDeserializer
{
    private readonly ConditionRegistry _registry;

    public ConditionDeserializer(ConditionRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public ICondition Deserialize(JsonElement json, Type stateType)
    {
        ArgumentNullException.ThrowIfNull(stateType);

        var type = json.GetProperty("type").GetString()
                   ?? throw new JsonException("Property 'type' cannot be null.");
        return _registry.Resolve(type).Deserialize(json, stateType, this);
    }
}
