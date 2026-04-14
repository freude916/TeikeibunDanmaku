namespace TeikeibunDanmaku.Core.Blackboard;

public sealed class CombatRoomEnteredState : IBoardState
{
    [DataField("敌人数量")] public required int EnemyCount { get; init; }

    [DataField("敌人名称列表")] public required IReadOnlyList<string> EnemyNames { get; init; }
}
