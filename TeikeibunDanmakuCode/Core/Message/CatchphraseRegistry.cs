namespace TeikeibunDanmaku.Core.Message;

public static class CatchphraseRegistry
{
    private static readonly Dictionary<string, CatchphraseEntry> Entries = new(StringComparer.Ordinal)
    {
        ["i_say"] = new CatchphraseEntry("口癖前缀（我说）",
            ["", "我说"],
            ["", "有没有懂得", "你尔多隆吗"]),
        ["character_general"] = new CatchphraseEntry("角色前缀（有一个角色）",
            ["有一个角色"],
            ["", "你知道ta是谁", "你不知道ta是谁"])
    };

    public static bool ContainsKey(string key)
    {
        return Entries.ContainsKey(key);
    }

    public static IReadOnlyList<CatchphraseTemplateInfo> ListTemplates()
    {
        return Entries
            .Select(pair => new CatchphraseTemplateInfo(pair.Key, BuildToken(pair.Key), pair.Value.DisplayName))
            .ToArray();
    }

    public static string GetDefaultTemplateToken()
    {
        var first = Entries.Keys.FirstOrDefault();
        return string.IsNullOrWhiteSpace(first) ? string.Empty : BuildToken(first);
    }

    public static CatchphraseRenderResult Pick(string key)
    {
        if (!Entries.TryGetValue(key, out var entry))
        {
            throw new InvalidOperationException($"Unknown catchphrase key '{key}'.");
        }

        var prefix = PickOne(entry.Prefixes);
        if (string.IsNullOrEmpty(prefix))
        {
            return new CatchphraseRenderResult(prefix, string.Empty);
        }

        var suffix = PickOne(entry.Suffixes);
        return new CatchphraseRenderResult(prefix, suffix);
    }

    private static string BuildToken(string key) => "{{" + key + "}}";

    private static string PickOne(IReadOnlyList<string> entries)
    {
        if (entries.Count == 0)
        {
            return string.Empty;
        }

        var index = Random.Shared.Next(entries.Count);
        return entries[index];
    }

    private sealed record CatchphraseEntry(
        string DisplayName,
        string[] Prefixes,
        string[] Suffixes);
}

public readonly record struct CatchphraseTemplateInfo(string Key, string Token, string DisplayName);

public readonly record struct CatchphraseRenderResult(string Prefix, string Suffix);