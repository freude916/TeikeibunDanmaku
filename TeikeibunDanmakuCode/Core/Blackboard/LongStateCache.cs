using System.Collections;
using MegaCrit.Sts2.Core.Runs;

namespace TeikeibunDanmaku.Core.Blackboard;

public static class LongStateCache
{
    private static readonly object Gate = new();

    private static int _runPlayerHp;
    private static int _runPlayerGold;
    private static int _runPlayerDeckSize;

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

    public static void Reset()
    {
        lock (Gate)
        {
            _runPlayerHp = 0;
            _runPlayerGold = 0;
            _runPlayerDeckSize = 0;
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
        var deckSize = ReadDeckSize(player);

        lock (Gate)
        {
            _runPlayerHp = hp;
            _runPlayerGold = gold;
            _runPlayerDeckSize = deckSize;
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

    private static int ReadDeckSize(object? player)
    {
        if (player == null)
            return 0;

        var playerType = player.GetType();
        var deck = playerType.GetProperty("Deck")?.GetValue(player) ??
                   playerType.GetProperty("MasterDeck")?.GetValue(player);

        if (deck == null)
            return 0;

        var countProperty = deck.GetType().GetProperty("Count");
        if (countProperty?.GetValue(deck) is int count)
            return count;

        if (deck is ICollection collection)
            return collection.Count;

        if (deck is IEnumerable enumerable)
            return enumerable.Cast<object>().Count();

        return 0;
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
}
