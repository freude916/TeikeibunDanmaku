using System.Reflection;

namespace TeikeibunDanmaku.Core.Blackboard;

public static class LongStateFieldRegistry
{
    private static readonly IReadOnlyDictionary<string, BoardFieldDescriptor> Descriptors =
        new Dictionary<string, BoardFieldDescriptor>(StringComparer.Ordinal)
        {
            [RunPlayerHpKey] = BuildNumericDescriptor(
                RunPlayerHpKey,
                "玩家血量",
                nameof(SyntheticFields.RunPlayerHp),
                _ => LongStateCache.RunPlayerHp
            ),
            [RunPlayerGoldKey] = BuildNumericDescriptor(
                RunPlayerGoldKey,
                "玩家金币",
                nameof(SyntheticFields.RunPlayerGold),
                _ => LongStateCache.RunPlayerGold
            ),
            [RunPlayerDeckSizeKey] = BuildNumericDescriptor(
                RunPlayerDeckSizeKey,
                "牌组数量",
                nameof(SyntheticFields.RunPlayerDeckSize),
                _ => LongStateCache.RunPlayerDeckSize
            )
        };

    public const string RunPlayerHpKey = "run.player_hp";
    public const string RunPlayerGoldKey = "run.player_gold";
    public const string RunPlayerDeckSizeKey = "run.player_deck_size";

    public static IReadOnlyDictionary<string, BoardFieldDescriptor> GetDescriptors()
    {
        return Descriptors;
    }

    private static BoardFieldDescriptor BuildNumericDescriptor(
        string key,
        string displayName,
        string syntheticPropertyName,
        Func<IBoardState, object?> getter)
    {
        var property = typeof(SyntheticFields).GetProperty(
            syntheticPropertyName,
            BindingFlags.Public | BindingFlags.Instance
        ) ?? throw new InvalidOperationException($"Synthetic property '{syntheticPropertyName}' was not found.");

        return new BoardFieldDescriptor
        {
            Name = key,
            DisplayName = displayName,
            ValueType = typeof(int),
            PropertyInfo = property,
            Getter = getter
        };
    }

    private sealed class SyntheticFields
    {
        public int RunPlayerHp => 0;
        public int RunPlayerGold => 0;
        public int RunPlayerDeckSize => 0;
    }
}
