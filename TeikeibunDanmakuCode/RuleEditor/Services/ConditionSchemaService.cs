using TeikeibunDanmaku.Core.Blackboard;
using TeikeibunDanmaku.Core.Condition;
using TeikeibunDanmaku.Timepoints;
using TeikeibunDanmaku.Utils;

namespace TeikeibunDanmaku.RuleEditor.Services;

public sealed class ConditionSchemaService
{
    private readonly TimepointStateResolver _stateResolver = new();
    private readonly ConditionRegistry _registry = ConditionRegistry.CreateDefault();

    public IReadOnlyList<string> ListConditionTypes()
    {
        return _registry.ListTypes();
    }

    public IReadOnlyList<string> GetAllowedKeys(string timepointId, string conditionType)
    {
        var stateType = _stateResolver.ResolveStateType(timepointId);
        var descriptors = BoardStateRegistry.GetFieldDescriptors(stateType);

        IEnumerable<string> keys = conditionType switch
        {
            ConditionType.Lt or ConditionType.Gt => descriptors
                .Where(pair => TypeUtil.IsNumericType(pair.Value.ValueType))
                .Select(pair => pair.Key),
            ConditionType.Find => descriptors
                .Where(pair => TypeUtil.GetNonNullableType(pair.Value.ValueType) == typeof(string))
                .Select(pair => pair.Key),
            ConditionType.Eq => descriptors.Keys,
            _ => []
        };

        return keys.OrderBy(key => key, StringComparer.OrdinalIgnoreCase).ToArray();
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
        if (node.Type is ConditionType.And or ConditionType.Or)
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
                ConditionType.Lt or ConditionType.Gt => "0",
                ConditionType.Find => string.Empty,
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
