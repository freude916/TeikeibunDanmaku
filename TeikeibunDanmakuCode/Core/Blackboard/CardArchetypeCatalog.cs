using MegaCrit.Sts2.Core.Models;
using TeikeibunDanmaku.Io;

namespace TeikeibunDanmaku.Core.Blackboard;

public static class CardArchetypeCatalog
{
    private static readonly CardArchetypeProfile NoneProfile = new([], false);
    private static readonly StringComparer IdComparer = StringComparer.OrdinalIgnoreCase;
    private static readonly object Gate = new();

    private static IReadOnlyDictionary<string, CardArchetypeProfile> _profilesById =
        new Dictionary<string, CardArchetypeProfile>(IdComparer);

    private static IReadOnlyList<CardKeywordProfile> _keywordProfiles = [];

    public static void ReloadFromDirectory(string rulesDirectoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rulesDirectoryPath);

        var cards = CardArchetypeJsoncIo.ImportFromDirectory(rulesDirectoryPath);

        var byId = new Dictionary<string, CardArchetypeProfile>(IdComparer);
        var byKeyword = new List<CardKeywordProfile>();

        foreach (var card in cards)
        {
            ValidateCardEntry(card);

            var profile = new CardArchetypeProfile(
                [.. card.Archetypes.Where(text => !string.IsNullOrWhiteSpace(text)).Select(text => text.Trim())],
                card.IsTerminal);

            if (!byId.TryAdd(card.ModelId.Trim(), profile))
            {
                throw new InvalidOperationException($"Duplicate card model_id '{card.ModelId}' in *.cards.jsonc files.");
            }

            var keywords = card.TitleKeywords
                ?.Where(text => !string.IsNullOrWhiteSpace(text))
                .Select(text => text.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (keywords is { Length: > 0 })
            {
                byKeyword.Add(new CardKeywordProfile(keywords, profile));
            }
        }

        lock (Gate)
        {
            _profilesById = byId;
            _keywordProfiles = byKeyword;
        }
    }

    public static CardArchetypeProfile Resolve(CardModel card)
    {
        ArgumentNullException.ThrowIfNull(card);
        return Resolve(card.Id.Entry, card.TitleLocString.GetFormattedText());
    }

    public static CardArchetypeProfile Resolve(string? modelId, string? title)
    {
        if (!string.IsNullOrWhiteSpace(modelId))
        {
            var key = modelId.Trim();
            if (_profilesById.TryGetValue(key, out var byId))
            {
                return byId;
            }
        }

        if (!string.IsNullOrWhiteSpace(title))
        {
            foreach (var keywordProfile in _keywordProfiles)
            {
                if (keywordProfile.MatchesTitle(title))
                {
                    return keywordProfile.Profile;
                }
            }
        }

        return NoneProfile;
    }

    private static void ValidateCardEntry(CardArchetypeDto card)
    {
        if (string.IsNullOrWhiteSpace(card.ModelId))
        {
            throw new InvalidOperationException("cards[].model_id cannot be empty.");
        }

        if (card.Archetypes == null)
        {
            throw new InvalidOperationException($"cards[].archetypes cannot be null (model_id='{card.ModelId}').");
        }
    }

    private readonly record struct CardKeywordProfile(string[] Keywords, CardArchetypeProfile Profile)
    {
        public bool MatchesTitle(string title)
        {
            foreach (var keyword in Keywords)
            {
                if (title.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

public readonly record struct CardArchetypeProfile(IReadOnlyList<string> Archetypes, bool IsTerminal)
{
}
