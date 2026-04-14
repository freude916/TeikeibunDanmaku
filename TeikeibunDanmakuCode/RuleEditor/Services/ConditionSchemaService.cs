using TeikeibunDanmaku.Core.Blackboard;
using TeikeibunDanmaku.Core.Condition;
using TeikeibunDanmaku.Timepoints;
using TeikeibunDanmaku.Utils;

namespace TeikeibunDanmaku.RuleEditor.Services;

public sealed class ConditionSchemaService
{
    private readonly TimepointStateResolver _stateResolver = new();
    private readonly ConditionRegistry _registry = ConditionRegistry.CreateDefault();

    public IReadOnlyList<TimepointDescriptor> ListTimepoints()
    {
        return _stateResolver.ListTimepoints();
    }

    public string GetTimepointDisplayName(string timepointId)
    {
        return _stateResolver.GetDisplayName(timepointId);
    }

    public string GetConditionTypeDisplayName(string conditionType)
    {
        return ConditionType.GetDisplayName(conditionType);
    }

    public IReadOnlyList<string> ListConditionTypes()
    {
        return _registry.ListTypes();
    }

    public IReadOnlyList<string> GetAllowedKeys(string timepointId, string conditionType)
    {
        var descriptors = GetAllowedFieldDescriptors(timepointId, conditionType);
        return descriptors.Select(item => item.Name).ToArray();
    }

    public IReadOnlyList<BoardFieldDescriptor> GetAllowedFieldDescriptors(string timepointId, string conditionType)
    {
        var stateType = _stateResolver.ResolveStateType(timepointId);
        var descriptors = BoardStateRegistry.GetFieldDescriptors(stateType);

        IEnumerable<BoardFieldDescriptor> fields = conditionType switch
        {
            ConditionType.ValueLt or ConditionType.ValueGt => descriptors
                .Where(pair => TypeUtil.IsNumericType(pair.Value.ValueType))
                .Select(pair => pair.Value),
            ConditionType.StrFind => descriptors
                .Where(pair => TypeUtil.GetNonNullableType(pair.Value.ValueType) == typeof(string))
                .Select(pair => pair.Value),
            ConditionType.Eq => descriptors.Values,
            _ => []
        };

        return fields
            .OrderBy(field => field.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(field => field.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public string GetFieldDisplayName(string timepointId, string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return string.Empty;
        }

        var stateType = _stateResolver.ResolveStateType(timepointId);
        var descriptors = BoardStateRegistry.GetFieldDescriptors(stateType);
        return descriptors.TryGetValue(key, out var descriptor) ? descriptor.DisplayName : key;
    }

    public ConditionDto CreateDefaultLeaf(string timepointId)
    {
        var key = GetAllowedKeys(timepointId, ConditionType.Eq).FirstOrDefault();
        return new ConditionDto
        {
            Type = ConditionType.Eq,
            Key = key,
            Value = "0"
        };
    }

    public ConditionDto NormalizeNode(string timepointId, ConditionDto node)
    {
        if (node.Type is ConditionType.CondAnd or ConditionType.CondOr)
        {
            var children = node.Conditions?.ToArray() ?? [];
            if (children.Length == 0)
            {
                children = [CreateDefaultLeaf(timepointId)];
            }

            return new ConditionDto
            {
                Type = node.Type,
                Conditions = children.Select(child => NormalizeNode(timepointId, child)).ToArray()
            };
        }

        var allowedKeys = GetAllowedKeys(timepointId, node.Type);
        var key = !string.IsNullOrWhiteSpace(node.Key) && allowedKeys.Contains(node.Key)
            ? node.Key
            : allowedKeys.FirstOrDefault();

        var value = node.Value;
        if (value == null)
        {
            value = node.Type switch
            {
                ConditionType.ValueLt or ConditionType.ValueGt => "0",
                ConditionType.StrFind => string.Empty,
                _ => "0"
            };
        }

        return new ConditionDto
        {
            Type = node.Type,
            Key = key,
            Value = value
        };
    }
}
