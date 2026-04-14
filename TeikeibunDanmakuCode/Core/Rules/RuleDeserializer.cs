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

    public Rule DeserializeJson(JsonElement json)
    {
        var dto = JsonSerializer.Deserialize<RuleDto>(json.GetRawText())
                  ?? throw new JsonException("Failed to deserialize rule DTO.");
        return DeserializeDto(dto);
    }

    public Rule DeserializeDto(RuleDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var ruleId = dto.RuleId ?? throw new JsonException("Property 'rule_id' cannot be null.");
        var timepointId = dto.Timepoint ?? throw new JsonException("Property 'timepoint' cannot be null.");
        var conditionDto = dto.Condition ?? throw new JsonException("Property 'condition' cannot be null.");
        var stateType = _timepointStateResolver.ResolveStateType(timepointId);
        var condition = _conditionDeserializer.DeserializeDto(conditionDto, stateType);
        var messages = ParseMessages(dto.Messages);
        MessageTemplateRenderer.ValidateTemplates(messages, stateType);

        return new Rule
        {
            RuleId = ruleId,
            TimepointId = timepointId,
            Condition = condition,
            Messages = messages
        };
    }

    private static string[] ParseMessages(IEnumerable<string> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);
        return [.. messages.Select(m => m ?? throw new JsonException("Rule message cannot be null."))];
    }
}
