using System.Text.Json.Serialization;

namespace TeikeibunDanmaku.Core.Condition;

public sealed class ConditionDto
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("key")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Key { get; init; }

    [JsonPropertyName("value")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Value { get; init; }

    [JsonPropertyName("conditions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<ConditionDto>? Conditions { get; init; }
}
