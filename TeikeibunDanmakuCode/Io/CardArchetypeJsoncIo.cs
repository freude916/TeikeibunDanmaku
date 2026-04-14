using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace TeikeibunDanmaku.Io;

public static class CardArchetypeJsoncIo
{
    public const string CardFileSuffix = ".cards.jsonc";

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

    public static IReadOnlyList<string> ListCardFiles(string rulesDirectoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rulesDirectoryPath);

        if (!Directory.Exists(rulesDirectoryPath))
        {
            return [];
        }

        return Directory
            .EnumerateFiles(rulesDirectoryPath, "*" + CardFileSuffix, SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static IReadOnlyList<CardArchetypeDto> ImportFromDirectory(string rulesDirectoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rulesDirectoryPath);

        var files = ListCardFiles(rulesDirectoryPath);
        var cards = new List<CardArchetypeDto>();

        foreach (var filePath in files)
        {
            cards.AddRange(ImportFromFile(filePath));
        }

        return cards;
    }

    public static IReadOnlyList<CardArchetypeDto> ImportFromFile(string filePath)
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
            throw new JsonException("Invalid Card Archetype Jsonc file");
        }

        return DeserializeDocument(document.RootElement).Cards;
    }

    public static void ExportToFile(string outputPath, IReadOnlyList<CardArchetypeDto> cards)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(cards);

        var directoryPath = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var payload = new CardArchetypeDocumentDto
        {
            Version = 1,
            Cards = [.. cards]
        };
        var json = JsonSerializer.Serialize(payload, JsonWriteOptions);
        var fileContent = "// TeikeibunDanmaku card archetypes (.cards.jsonc)\n" + json + Environment.NewLine;
        File.WriteAllText(outputPath, fileContent);
    }

    private static CardArchetypeDocumentDto DeserializeDocument(JsonElement root)
    {
        var document = JsonSerializer.Deserialize<CardArchetypeDocumentDto>(root.GetRawText())
                       ?? throw new JsonException("Failed to deserialize card archetype document.");
        return document.Cards == null ? throw new JsonException("Property 'cards' cannot be null.") : document;
    }

    private sealed class CardArchetypeDocumentDto
    {
        [JsonPropertyName("version")]
        public int Version { get; init; } = 1;

        [JsonPropertyName("cards")]
        public required CardArchetypeDto[] Cards { get; init; }
    }
}

public sealed class CardArchetypeDto
{
    [JsonPropertyName("model_id")]
    public required string ModelId { get; init; }

    [JsonPropertyName("archetypes")]
    public required string[] Archetypes { get; init; }

    [JsonPropertyName("is_terminal")]
    public bool IsTerminal { get; init; }

    [JsonPropertyName("title_keywords")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? TitleKeywords { get; init; }
}
