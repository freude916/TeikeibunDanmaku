using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using TeikeibunDanmaku.Core.Rules;
using TeikeibunDanmaku.Timepoints;

namespace TeikeibunDanmaku.Io;

public static class RuleJsoncIo
{
    public const string RuleFileSuffix = ".danmu.jsonc";

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
    public static IReadOnlyList<string> ListRuleFiles(string rulesDirectoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rulesDirectoryPath);

        if (!Directory.Exists(rulesDirectoryPath))
        {
            return [];
        }

        return Directory
            .EnumerateFiles(rulesDirectoryPath, "*" + RuleFileSuffix, SearchOption.TopDirectoryOnly)
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
        var files = ListRuleFiles(rulesDirectoryPath);

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
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Invalid Rule Jsonc file");
        }

        var dto = DeserializeDocument(document.RootElement);
        return dto.Rules;
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

        var payload = new RuleJsoncDocumentDto
        {
            Version = 1,
            Rules = [.. rules.Select(rule => rule.Serialize())]
        };
        var json = JsonSerializer.Serialize(payload, JsonWriteOptions);
        var fileContent = "// TeikeibunDanmaku rules (.danmu.jsonc)\n" + json + Environment.NewLine;
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

        var payload = new RuleJsoncDocumentDto
        {
            Version = 1,
            Rules = [.. rules]
        };
        var json = JsonSerializer.Serialize(payload, JsonWriteOptions);
        var fileContent = "// TeikeibunDanmaku rules (.danmu.jsonc)\n" + json + Environment.NewLine;
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
        return document.RootElement.ValueKind != JsonValueKind.Object ? 
            throw new JsonException("Invalid Rule Jsonc file") : 
            [.. DeserializeDocument(document.RootElement).Rules.Select(rule => deserializer.DeserializeDto(rule))];
    }

    private static RuleJsoncDocumentDto DeserializeDocument(JsonElement element)
    {
        var document = JsonSerializer.Deserialize<RuleJsoncDocumentDto>(element.GetRawText())
                       ?? throw new JsonException("Failed to deserialize rule document.");
        return document.Rules == null ? throw new JsonException("Property 'rules' cannot be null.") : document;
    }
    #endregion

    private sealed class RuleJsoncDocumentDto
    {
        [JsonPropertyName("version")]
        public int Version { get; init; } = 1;

        [JsonPropertyName("rules")]
        public required RuleDto[] Rules { get; init; }
    }
}
