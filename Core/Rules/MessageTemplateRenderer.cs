using System.Text.RegularExpressions;
using TeikeibunDanmaku.Core.Blackboard;

namespace TeikeibunDanmaku.Core.Rules;

public static class MessageTemplateRenderer
{
    private static readonly Regex PlaceholderRegex = new(@"\$\{(?<key>[A-Za-z_][A-Za-z0-9_]*)\}", RegexOptions.Compiled);

    public static string Render(string template, IBoardState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(template);

        var descriptors = state.FieldDescriptors;
        return PlaceholderRegex.Replace(template, match =>
        {
            var key = match.Groups["key"].Value;
            if (!descriptors.TryGetValue(key, out var descriptor))
            {
                throw new InvalidOperationException($"Message template key '{key}' was not found in state '{state.GetType().Name}'.");
            }

            var value = descriptor.Getter(state);
            return value?.ToString() ?? string.Empty;
        });
    }

    public static void ValidateTemplates(IReadOnlyList<string> templates, Type stateType)
    {
        ArgumentNullException.ThrowIfNull(templates);
        ArgumentNullException.ThrowIfNull(stateType);

        var descriptors = BoardStateRegistry.GetFieldDescriptors(stateType);
        for (var i = 0; i < templates.Count; i++)
        {
            var template = templates[i] ?? throw new InvalidOperationException($"Rule message at index {i} cannot be null.");
            foreach (Match match in PlaceholderRegex.Matches(template))
            {
                var key = match.Groups["key"].Value;
                if (!descriptors.ContainsKey(key))
                {
                    throw new InvalidOperationException(
                        $"Message template key '{key}' was not found in state type '{stateType.Name}'. MessageIndex={i}");
                }
            }
        }
    }
}
