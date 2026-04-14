using TeikeibunDanmaku.Core.Condition;
using TeikeibunDanmaku.Core.Rules;

namespace TeikeibunDanmaku.RuleEditor.Model;

public static class RuleEditorDtoExtensions
{
    public static RuleDto Clone(this RuleDto rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        return new RuleDto
        {
            RuleId = rule.RuleId,
            Timepoint = rule.Timepoint,
            Messages = [.. rule.Messages],
            Condition = rule.Condition.Clone()
        };
    }

    public static ConditionDto Clone(this ConditionDto condition)
    {
        ArgumentNullException.ThrowIfNull(condition);

        return new ConditionDto
        {
            Type = condition.Type,
            Key = condition.Key,
            Value = condition.Value,
            Conditions = condition.Conditions?.Select(Clone).ToArray()
        };
    }

    public static bool TryGetNodeByPath(this ConditionDto root, IReadOnlyList<int> path, out ConditionDto node)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(path);

        node = root;
        foreach (var index in path)
        {
            var children = node.Conditions?.ToArray();
            if (children == null || index < 0 || index >= children.Length)
            {
                node = null!;
                return false;
            }

            node = children[index];
        }

        return true;
    }

    public static ConditionDto ReplaceAtPath(this ConditionDto root, IReadOnlyList<int> path, ConditionDto replacement)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(replacement);

        if (path.Count == 0)
        {
            return replacement.Clone();
        }

        var clone = root.Clone();
        var parentPath = path.Take(path.Count - 1).ToArray();
        if (!clone.TryGetNodeByPath(parentPath, out var parent))
        {
            throw new InvalidOperationException("Invalid condition path.");
        }

        var children = parent.Conditions?.Select(Clone).ToArray() ?? [];
        var index = path[^1];
        if (index < 0 || index >= children.Length)
        {
            throw new InvalidOperationException("Invalid condition path index.");
        }

        children[index] = replacement.Clone();
        var replacedParent = new ConditionDto
        {
            Type = parent.Type,
            Key = parent.Key,
            Value = parent.Value,
            Conditions = children
        };

        return clone.ReplaceAtPath(parentPath, replacedParent);
    }
}
