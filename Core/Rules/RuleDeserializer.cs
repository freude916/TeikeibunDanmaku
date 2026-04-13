using System.Text.Json;
using TeikeibunDanmaku.Core.Condition;
using TeikeibunDanmaku.Timepoints;

namespace TeikeibunDanmaku.Core.Rules;

public sealed class RuleDeserializer(TimepointStateResolver timepointStateResolver, ConditionRegistry conditionRegistry)
{
    private readonly TimepointStateResolver _timepointStateResolver = timepointStateResolver ?? throw new ArgumentNullException(nameof(timepointStateResolver));
    private readonly ConditionDeserializer _conditionDeserializer = new(conditionRegistry ?? throw new ArgumentNullException(nameof(conditionRegistry)));

    public RuleDeserializer(TimepointStateResolver timepointStateResolver)
        : this(timepointStateResolver, ConditionRegistry.CreateDefault())
    {
    }

    public Rule Deserialize(JsonElement json)
    {
        var ruleId = json.GetProperty("rule_id").GetString()
                     ?? throw new JsonException("Property 'rule_id' cannot be null.");
        var timepointId = json.GetProperty("timepoint").GetString()
                          ?? throw new JsonException("Property 'timepoint' cannot be null.");
        var conditionElement = json.GetProperty("condition");
        var messagesElement = json.GetProperty("messages");
        var stateType = _timepointStateResolver.ResolveStateType(timepointId);
        var condition = _conditionDeserializer.Deserialize(conditionElement, stateType);
        var messages = ParseMessages(messagesElement);
        MessageTemplateRenderer.ValidateTemplates(messages, stateType);

        return new Rule
        {
            RuleId = ruleId,
            TimepointId = timepointId,
            Condition = condition,
            Messages = messages
        };
    }

    private static string[] ParseMessages(JsonElement messagesElement)
    {
        if (messagesElement.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("Property 'messages' must be an array.");
        }

        return [.. messagesElement
            .EnumerateArray()
            .Select(m => m.GetString() ?? throw new JsonException("Rule message cannot be null."))];
    }
}
