using System.Text.Json;
using TeikeibunDanmaku.Timepoints;

namespace TeikeibunDanmaku.Core.Rules;

public static class RuleJsoncIo
{
    private static readonly JsonDocumentOptions JsoncReadOptions = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private static readonly JsonSerializerOptions JsonWriteOptions = new()
    {
        WriteIndented = true
    };

    public static IReadOnlyList<Rule> ImportFromDirectory(string rulesDirectoryPath, TimepointStateResolver resolver)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rulesDirectoryPath);
        ArgumentNullException.ThrowIfNull(resolver);

        if (!Directory.Exists(rulesDirectoryPath))
        {
            return [];
        }

        var deserializer = new RuleDeserializer(resolver);
        var files = Directory
            .EnumerateFiles(rulesDirectoryPath, "*.jsonc", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var rules = new List<Rule>();
        foreach (var filePath in files)
        {
            try {
                rules.AddRange(ImportFile(filePath, deserializer));
            }
            catch (JsonException ex)
            {
                MainFile.Logger.Error($"Failed to import rules from file '{filePath}': {ex.Message}");
            }
        }

        return rules;
    }

    public static void ExportToFile(string outputPath, IReadOnlyList<Rule> rules)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(rules);

        var directoryPath = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var payload = rules.Select(rule => rule.Serialize()).ToArray();
        var json = JsonSerializer.Serialize(payload, JsonWriteOptions);
        var fileContent = "// TeikeibunDanmaku rules (.jsonc)\n" + json + Environment.NewLine;
        File.WriteAllText(outputPath, fileContent);
    }

    private static List<Rule> ImportFile(string filePath, RuleDeserializer deserializer)
    {
        var content = File.ReadAllText(filePath);
        if (string.IsNullOrWhiteSpace(content))
        {
            return [];
        }
        
        using var document = JsonDocument.Parse(content, JsoncReadOptions);
        return document.RootElement.ValueKind != JsonValueKind.Array ? 
            throw new JsonException("Invalid Rule Jsonc file") : 
            [.. document.RootElement.EnumerateArray().Select(deserializer.Deserialize)];
    }

}
