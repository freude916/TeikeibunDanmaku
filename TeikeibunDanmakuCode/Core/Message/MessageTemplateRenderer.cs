using System.Text.RegularExpressions;
using TeikeibunDanmaku.Core.Blackboard;
using TeikeibunDanmaku.Core.Message;

namespace TeikeibunDanmaku.Core.Rules;

public static class MessageTemplateRenderer
{
    private static readonly Regex CatchphraseRegex = new(@"\{\{(?<key>[A-Za-z_][A-Za-z0-9_]*)\}\}", RegexOptions.Compiled);
    private static readonly Regex PlaceholderRegex = new(@"\$\{(?<key>[A-Za-z_][A-Za-z0-9_]*)\}", RegexOptions.Compiled);

    public static string Render(string template, IBoardState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(template);

        var suffixes = new List<string>();
        var renderedTemplate = CatchphraseRegex.Replace(template, match =>
        {
            var key = match.Groups["key"].Value;
            if (!CatchphraseRegistry.ContainsKey(key))
            {
                throw new InvalidOperationException($"Message catchphrase key '{key}' is unknown.");
            }

            var picked = CatchphraseRegistry.Pick(key);
            if (!string.IsNullOrEmpty(picked.Prefix) && !string.IsNullOrEmpty(picked.Suffix))
            {
                suffixes.Add(picked.Suffix);
            }

            return picked.Prefix;
        });

        var descriptors = state.FieldDescriptors;
        var rendered = PlaceholderRegex.Replace(renderedTemplate, match =>
        {
            var key = match.Groups["key"].Value;
            if (!descriptors.TryGetValue(key, out var descriptor))
            {
                throw new InvalidOperationException($"Message template key '{key}' was not found in state '{state.GetType().Name}'.");
            }

            var value = descriptor.Getter(state);
            return value?.ToString() ?? string.Empty;
        });

        return suffixes.Count == 0 ? rendered : rendered + string.Concat(suffixes);
    }

    public static void ValidateTemplates(IReadOnlyList<string> templates, Type stateType)
    {
        ArgumentNullException.ThrowIfNull(templates);
        ArgumentNullException.ThrowIfNull(stateType);

        var descriptors = BoardStateRegistry.GetFieldDescriptors(stateType);
        for (var i = 0; i < templates.Count; i++)
        {
            var template = templates[i] ?? throw new InvalidOperationException($"Rule message at index {i} cannot be null.");
            foreach (Match catchphraseMatch in CatchphraseRegex.Matches(template))
            {
                var key = catchphraseMatch.Groups["key"].Value;
                if (!CatchphraseRegistry.ContainsKey(key))
                {
                    throw new InvalidOperationException(
                        $"Message catchphrase key '{key}' was not found. MessageIndex={i}");
                }
            }

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
