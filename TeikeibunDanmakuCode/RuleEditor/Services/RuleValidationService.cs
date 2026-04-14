using TeikeibunDanmaku.Core.Condition;
using TeikeibunDanmaku.Core.Rules;
using TeikeibunDanmaku.Timepoints;

namespace TeikeibunDanmaku.RuleEditor.Services;

public sealed class RuleValidationService
{
    private readonly TimepointStateResolver _stateResolver = new();
    private readonly ConditionDeserializer _conditionDeserializer = new(ConditionRegistry.CreateDefault());

    public ValidationResult ValidateAll(IReadOnlyList<RuleDto> rules)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < rules.Count; i++)
        {
            var rule = rules[i];
            var prefix = $"rule[{i}]";

            if (string.IsNullOrWhiteSpace(rule.RuleId))
            {
                return ValidationResult.Fail($"{prefix}.rule_id.empty");
            }

            if (!seen.Add(rule.RuleId.Trim()))
            {
                return ValidationResult.Fail($"{prefix}.rule_id.duplicate");
            }

            var singleResult = ValidateRule(rule, prefix);
            if (!singleResult.IsValid)
            {
                return singleResult;
            }
        }

        return ValidationResult.Success();
    }

    public ValidationResult ValidateRule(RuleDto rule, string path = "rule")
    {
        if (string.IsNullOrWhiteSpace(rule.Timepoint))
        {
            return ValidationResult.Fail($"{path}.timepoint.empty");
        }

        if (rule.Messages.Length == 0 || rule.Messages.Any(string.IsNullOrWhiteSpace))
        {
            return ValidationResult.Fail($"{path}.messages.invalid");
        }

        Type stateType;
        try
        {
            stateType = _stateResolver.ResolveStateType(rule.Timepoint);
        }
        catch
        {
            return ValidationResult.Fail($"{path}.timepoint.invalid");
        }

        return ValidateCondition(rule.Condition, stateType, $"{path}.condition");
    }

    public ValidationResult ValidateCondition(string timepointId, ConditionDto condition, string path = "condition")
    {
        Type stateType;
        try
        {
            stateType = _stateResolver.ResolveStateType(timepointId);
        }
        catch
        {
            return ValidationResult.Fail("condition.timepoint.invalid");
        }

        return ValidateCondition(condition, stateType, path);
    }

    private ValidationResult ValidateCondition(ConditionDto condition, Type stateType, string path)
    {
        if (string.IsNullOrWhiteSpace(condition.Type))
        {
            return ValidationResult.Fail($"{path}.type.empty");
        }

        if (condition.Type is ConditionType.CondAnd or ConditionType.CondOr)
        {
            var children = condition.Conditions?.ToArray() ?? [];
            if (children.Length == 0)
            {
                return ValidationResult.Fail($"{path}.conditions.empty");
            }

            for (var i = 0; i < children.Length; i++)
            {
                var childResult = ValidateCondition(children[i], stateType, $"{path}.conditions[{i}]");
                if (!childResult.IsValid)
                {
                    return childResult;
                }
            }

            return ValidationResult.Success();
        }

        if (string.IsNullOrWhiteSpace(condition.Key))
        {
            return ValidationResult.Fail($"{path}.key.empty");
        }

        try
        {
            _conditionDeserializer.DeserializeDto(condition, stateType);
            return ValidationResult.Success();
        }
        catch (Exception ex)
        {
            return ValidationResult.Fail($"{path}.invalid", ex.Message);
        }
    }
}

public sealed record ValidationResult(bool IsValid, string ErrorCode, string? Detail)
{
    public static ValidationResult Success() => new(true, string.Empty, null);

    public static ValidationResult Fail(string code, string? detail = null) => new(false, code, detail);
}
