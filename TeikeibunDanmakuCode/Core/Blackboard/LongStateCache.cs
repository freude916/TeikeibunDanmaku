using System.Collections;
using MegaCrit.Sts2.Core.Runs;

namespace TeikeibunDanmaku.Core.Blackboard;

public static class LongStateCache
{
    private static readonly object Gate = new();

    private static int _runPlayerHp;
    private static int _runPlayerGold;
    private static int _runPlayerDeckSize;
    private static string[] _runPlayerDeck = [];
    private static Dictionary<string, int> _runPlayerDeckArchetypeTable = new(StringComparer.Ordinal);
    private static string[] _runPlayerRelics = [];

    public static int RunPlayerHp
    {
        get
        {
            lock (Gate)
                return _runPlayerHp;
        }
    }

    public static int RunPlayerGold
    {
        get
        {
            lock (Gate)
                return _runPlayerGold;
        }
    }

    public static int RunPlayerDeckSize
    {
        get
        {
            lock (Gate)
                return _runPlayerDeckSize;
        }
    }

    public static IReadOnlyList<string> RunPlayerDeck
    {
        get
        {
            lock (Gate)
                return [.. _runPlayerDeck];
        }
    }

    public static IReadOnlyDictionary<string, int> RunPlayerDeckArchetypeTable
    {
        get
        {
            lock (Gate)
                return new Dictionary<string, int>(_runPlayerDeckArchetypeTable, StringComparer.Ordinal);
        }
    }

    public static IReadOnlyList<string> RunPlayerRelics
    {
        get
        {
            lock (Gate)
                return [.. _runPlayerRelics];
        }
    }

    public static void Reset()
    {
        lock (Gate)
        {
            _runPlayerHp = 0;
            _runPlayerGold = 0;
            _runPlayerDeckSize = 0;
            _runPlayerDeck = [];
            _runPlayerDeckArchetypeTable = new Dictionary<string, int>(StringComparer.Ordinal);
            _runPlayerRelics = [];
        }
    }

    public static void RefreshFromRunState(RunState? runState)
    {
        RefreshFromAny(TryGetPlayerFromRunState(runState));
    }

    public static void RefreshFromGame()
    {
        var runManager = RunManager.Instance;
        var runState = runManager?.State;
        RefreshFromRunState(runState);
    }

    private static void RefreshFromAny(object? player)
    {
        var hp = ReadInt(player, "CurrentHp");
        var gold = ReadInt(player, "Gold");
        var deckEntries = ReadDeckEntries(player);
        var deckSize = deckEntries.Count;
        var deckArchetypeTable = BuildDeckArchetypeTable(deckEntries);
        var relicEntries = ReadRelicEntries(player);

        lock (Gate)
        {
            _runPlayerHp = hp;
            _runPlayerGold = gold;
            _runPlayerDeckSize = deckSize;
            _runPlayerDeck = [.. deckEntries.Select(entry => entry.DisplayName)];
            _runPlayerDeckArchetypeTable = deckArchetypeTable;
            _runPlayerRelics = [.. relicEntries];
        }
    }

    private static object? TryGetPlayerFromRunState(object? runState)
    {
        if (runState == null)
            return null;

        var runStateType = runState.GetType();

        var localPlayer = runStateType.GetProperty("LocalPlayer")?.GetValue(runState);
        if (localPlayer != null)
            return localPlayer;

        var player = runStateType.GetProperty("Player")?.GetValue(runState);
        if (player != null)
            return player;

        var playersObject = runStateType.GetProperty("Players")?.GetValue(runState);
        if (playersObject is IEnumerable playersEnumerable)
        {
            foreach (var entry in playersEnumerable)
            {
                if (entry != null)
                    return entry;
            }
        }

        return null;
    }

    private static List<CardEntrySnapshot> ReadDeckEntries(object? player)
    {
        if (player == null)
            return [];

        var playerType = player.GetType();
        var deck = playerType.GetProperty("Deck")?.GetValue(player) ??
                   playerType.GetProperty("MasterDeck")?.GetValue(player);

        if (deck == null)
            return [];

        if (deck is not IEnumerable enumerable)
            return [];

        var cards = new List<CardEntrySnapshot>();
        foreach (var entry in enumerable)
        {
            if (entry == null)
                continue;

            var modelId = ReadIdEntry(entry);
            var title = ReadStringProperty(entry, "Title")
                        ?? ReadLocalizedText(entry, "TitleLocString");

            if (string.IsNullOrWhiteSpace(modelId) && string.IsNullOrWhiteSpace(title))
                continue;

            var displayName = !string.IsNullOrWhiteSpace(modelId) ? modelId!.Trim() : title!.Trim();
            cards.Add(new CardEntrySnapshot(modelId?.Trim(), title?.Trim(), displayName));
        }

        return cards;
    }

    private static List<string> ReadRelicEntries(object? player)
    {
        if (player == null)
            return [];

        var relics = player.GetType().GetProperty("Relics")?.GetValue(player);
        if (relics is not IEnumerable enumerable)
            return [];

        var result = new List<string>();
        foreach (var entry in enumerable)
        {
            if (entry == null)
                continue;

            var id = ReadIdEntry(entry);
            if (!string.IsNullOrWhiteSpace(id))
            {
                result.Add(id.Trim());
                continue;
            }

            var title = ReadStringProperty(entry, "Title")
                        ?? ReadLocalizedText(entry, "TitleLocString")
                        ?? ReadStringProperty(entry, "Name");
            if (!string.IsNullOrWhiteSpace(title))
            {
                result.Add(title.Trim());
            }
        }

        return result;
    }

    private static Dictionary<string, int> BuildDeckArchetypeTable(IEnumerable<CardEntrySnapshot> entries)
    {
        var table = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            var profile = CardArchetypeCatalog.Resolve(entry.ModelId, entry.Title);
            foreach (var archetype in profile.Archetypes)
            {
                if (string.IsNullOrWhiteSpace(archetype))
                    continue;

                var key = archetype.Trim();
                table[key] = table.GetValueOrDefault(key) + 1;
            }
        }

        return table;
    }

    private static int ReadInt(object? source, string propertyName)
    {
        if (source == null || string.IsNullOrWhiteSpace(propertyName))
            return 0;

        var value = source.GetType().GetProperty(propertyName)?.GetValue(source);
        return value switch
        {
            byte b => b,
            sbyte sb => sb,
            short s => s,
            ushort us => us,
            int i => i,
            uint ui => unchecked((int)ui),
            long l => unchecked((int)l),
            ulong ul => unchecked((int)ul),
            _ => 0
        };
    }

    private static string? ReadIdEntry(object source)
    {
        var idObject = source.GetType().GetProperty("Id")?.GetValue(source);
        if (idObject == null)
            return null;

        return idObject.GetType().GetProperty("Entry")?.GetValue(idObject) as string;
    }

    private static string? ReadStringProperty(object source, string propertyName)
    {
        var value = source.GetType().GetProperty(propertyName)?.GetValue(source);
        return value as string;
    }

    private static string? ReadLocalizedText(object source, string propertyName)
    {
        var localized = source.GetType().GetProperty(propertyName)?.GetValue(source);
        var text = localized?.GetType().GetMethod("GetFormattedText", Type.EmptyTypes)?.Invoke(localized, null);
        return text as string;
    }

    private readonly record struct CardEntrySnapshot(string? ModelId, string? Title, string DisplayName);
}
