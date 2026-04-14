using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using TeikeibunDanmaku.Core.Rules;
using TeikeibunDanmaku.Timepoints;

namespace TeikeibunDanmaku.Io;

public static class RuleJsoncIo
{
    #region Json Options
    private static readonly JsonDocumentOptions JsoncReadOptions = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private static readonly JsonSerializerOptions JsonWriteOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };
    #endregion

    #region File Discovery
    public static IReadOnlyList<string> ListJsoncFiles(string rulesDirectoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rulesDirectoryPath);

        if (!Directory.Exists(rulesDirectoryPath))
        {
            return [];
        }

        return Directory
            .EnumerateFiles(rulesDirectoryPath, "*.jsonc", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
    #endregion

    #region Import
    public static IReadOnlyList<Rule> ImportFromDirectory(string rulesDirectoryPath, TimepointStateResolver resolver)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rulesDirectoryPath);
        ArgumentNullException.ThrowIfNull(resolver);

        var deserializer = new RuleDeserializer(resolver);
        var files = ListJsoncFiles(rulesDirectoryPath);

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

    public static IReadOnlyList<Rule> ImportFromFile(string filePath, TimepointStateResolver resolver)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(resolver);

        if (!File.Exists(filePath))
        {
            return [];
        }

        var deserializer = new RuleDeserializer(resolver);
        return ImportFile(filePath, deserializer);
    }

    public static IReadOnlyList<RuleDto> ImportDtosFromFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            return [];
        }

        var content = File.ReadAllText(filePath);
        if (string.IsNullOrWhiteSpace(content))
        {
            return [];
        }

        using var document = JsonDocument.Parse(content, JsoncReadOptions);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("Invalid Rule Jsonc file");
        }

        return [.. document.RootElement.EnumerateArray().Select(DeserializeRuleDto)];
    }
    #endregion

    #region Export
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

    public static void ExportDtosToFile(string outputPath, IReadOnlyList<RuleDto> rules)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(rules);

        var directoryPath = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var json = JsonSerializer.Serialize(rules, JsonWriteOptions);
        var fileContent = "// TeikeibunDanmaku rules (.jsonc)\n" + json + Environment.NewLine;
        File.WriteAllText(outputPath, fileContent);
    }
    #endregion

    #region Internals
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
            [.. document.RootElement.EnumerateArray().Select(deserializer.DeserializeJson)];
    }

    private static RuleDto DeserializeRuleDto(JsonElement element)
    {
        return JsonSerializer.Deserialize<RuleDto>(element.GetRawText())
               ?? throw new JsonException("Failed to deserialize RuleDto.");
    }
    #endregion
}
