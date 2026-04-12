using System.Text.Json;

namespace TeikeibunDanmaku.Core.Rules;

public sealed class RuleDeserializer
{
    private readonly ITimepointStateResolver _timepointStateResolver;

    public RuleDeserializer(ITimepointStateResolver timepointStateResolver)
    {
        _timepointStateResolver = timepointStateResolver ?? throw new ArgumentNullException(nameof(timepointStateResolver));
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
        var condition = ConditionDeserializer.Deserialize(conditionElement, stateType);
        var messages = ParseMessages(messagesElement);

        return new Rule
        {
            RuleId = ruleId,
            TimepointId = timepointId,
            Condition = condition,
            Messages = messages
        };
    }
    
    private static IReadOnlyList<string> ParseMessages(JsonElement messagesElement)
    {
        if (messagesElement.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("Property 'messages' must be an array.");
        }

        return messagesElement
            .EnumerateArray()
            .Select(m => m.GetString() ?? throw new JsonException("Rule message cannot be null."))
            .ToArray();
    }
}
