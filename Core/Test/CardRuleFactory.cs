using System.Text.Json;
using System.Text.Json.Serialization;
using MegaCrit.Sts2.Core.Models;
using TeikeibunDanmaku.Core.Timepoints;
using TeikeibunDanmaku.Core.Rules;

namespace TeikeibunDanmaku.Core.Test;

public static class CardRuleFactory
{
    public static Rule CreateRewardSeenRule(CardModel cardModel, IReadOnlyList<string> messages)
    {
        ArgumentNullException.ThrowIfNull(cardModel);
        return CreateRewardSeenRule(cardModel.Id.Entry, messages);
    }

    public static Rule CreateRewardSeenRule(string modelId, IReadOnlyList<string> messages)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new ArgumentException("ModelId cannot be null or whitespace.", nameof(modelId));
        }

        ArgumentNullException.ThrowIfNull(messages);

        var payload = new RulePayload
        {
            RuleId = $"test.reward_seen.{modelId}",
            Timepoint = RewardSeenTimepoint.TimepointId,
            Condition = new ConditionPayload
            {
                Type = ConditionType.Eq,
                Key = "ModelId",
                Value = modelId
            },
            Messages = messages.ToArray()
        };

        // Test path requirement: always round-trip through JSON.
        var json = JsonSerializer.Serialize(payload);
        using var document = JsonDocument.Parse(json);
        var deserializer = new RuleDeserializer(new TimepointStateResolver());
        return deserializer.Deserialize(document.RootElement);
    }

    private sealed class RulePayload
    {
        [JsonPropertyName("rule_id")]
        public required string RuleId { get; init; }

        [JsonPropertyName("timepoint")]
        public required string Timepoint { get; init; }

        [JsonPropertyName("condition")]
        public required ConditionPayload Condition { get; init; }

        [JsonPropertyName("messages")]
        public required string[] Messages { get; init; }
    }

    private sealed class ConditionPayload
    {
        [JsonPropertyName("type")]
        public required string Type { get; init; }

        [JsonPropertyName("key")]
        public required string Key { get; init; }

        [JsonPropertyName("value")]
        public required string Value { get; init; }
    }
}
