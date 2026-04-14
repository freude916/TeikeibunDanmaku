namespace TeikeibunDanmaku.Core.Blackboard;

public static class LongStateFieldRegistry
{
    private static readonly IReadOnlyDictionary<string, BoardFieldDescriptor> Descriptors = BuildDescriptors();

    public static IReadOnlyDictionary<string, BoardFieldDescriptor> GetDescriptors()
    {
        return Descriptors;
    }

    private static IReadOnlyDictionary<string, BoardFieldDescriptor> BuildDescriptors()
    {
        var dataFieldDescriptors = BoardStateRegistry.GetFieldDescriptors(typeof(LongStateBoard));
        var resolved = new Dictionary<string, BoardFieldDescriptor>(StringComparer.Ordinal);

        foreach (var descriptor in dataFieldDescriptors.Values)
        {
            var externalKey = ToExternalKey(descriptor.Name);
            resolved[externalKey] = new BoardFieldDescriptor
            {
                Name = externalKey,
                DisplayName = descriptor.DisplayName,
                ValueType = descriptor.ValueType,
                PropertyInfo = descriptor.PropertyInfo,
                Getter = BuildGetter(descriptor.Name)
            };
        }

        return resolved;
    }

    private static Func<IBoardState, object?> BuildGetter(string propertyName)
    {
        return _ => propertyName switch
        {
            nameof(LongStateBoard.RunPlayerHp) => LongStateCache.RunPlayerHp,
            nameof(LongStateBoard.RunPlayerGold) => LongStateCache.RunPlayerGold,
            nameof(LongStateBoard.RunPlayerDeckSize) => LongStateCache.RunPlayerDeckSize,
            nameof(LongStateBoard.RunPlayerDeck) => LongStateCache.RunPlayerDeck,
            nameof(LongStateBoard.RunPlayerDeckArchetypeTable) => LongStateCache.RunPlayerDeckArchetypeTable,
            nameof(LongStateBoard.RunPlayerRelics) => LongStateCache.RunPlayerRelics,
            _ => null
        };
    }

    private static string ToExternalKey(string propertyName)
    {
        return propertyName switch
        {
            nameof(LongStateBoard.RunPlayerHp) => "run.player_hp",
            nameof(LongStateBoard.RunPlayerGold) => "run.player_gold",
            nameof(LongStateBoard.RunPlayerDeckSize) => "run.player_deck_size",
            nameof(LongStateBoard.RunPlayerDeck) => "run.player_deck",
            nameof(LongStateBoard.RunPlayerDeckArchetypeTable) => "run.player_deck_archetype_table",
            nameof(LongStateBoard.RunPlayerRelics) => "run.player_relics",
            _ => throw new InvalidOperationException($"Unknown long state property '{propertyName}'.")
        };
    }

    private sealed class LongStateBoard : IBoardState
    {
        [DataField("玩家血量")]
        public int RunPlayerHp => 0;

        [DataField("玩家金币")]
        public int RunPlayerGold => 0;

        [DataField("牌组数量")]
        public int RunPlayerDeckSize => 0;

        [DataField("牌组列表")]
        public IReadOnlyList<string> RunPlayerDeck => [];

        [DataField("牌组流派表")]
        public IReadOnlyDictionary<string, int> RunPlayerDeckArchetypeTable => new Dictionary<string, int>(StringComparer.Ordinal);

        [DataField("遗物列表")]
        public IReadOnlyList<string> RunPlayerRelics => [];
    }
}
