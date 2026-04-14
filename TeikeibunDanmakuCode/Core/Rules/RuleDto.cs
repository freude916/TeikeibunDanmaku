using System.Text.Json.Serialization;
using TeikeibunDanmaku.Core.Condition;

namespace TeikeibunDanmaku.Core.Rules;

public sealed class RuleDto
{
    [JsonPropertyName("rule_id")]
    public required string RuleId { get; init; }

    [JsonPropertyName("timepoint")]
    public required string Timepoint { get; init; }

    [JsonPropertyName("condition")]
    public required ConditionDto Condition { get; init; }

    [JsonPropertyName("messages")]
    public required string[] Messages { get; init; }
}
