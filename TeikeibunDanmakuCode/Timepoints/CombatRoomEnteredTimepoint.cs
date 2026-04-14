using TeikeibunDanmaku.Core.Blackboard;

namespace TeikeibunDanmaku.Timepoints;

public sealed class CombatRoomEnteredTimepoint : Timepoint<CombatRoomEnteredState>
{
    public const string TimepointId = "combat.room_entered";
    public const string TimepointDisplayName = "进入战斗房间";

    public override string Id => TimepointId;
    public override string DisplayName => TimepointDisplayName;

    public static CombatRoomEnteredTimepoint FromEnemies(IReadOnlyList<string> enemyNames)
    {
        ArgumentNullException.ThrowIfNull(enemyNames);

        return new CombatRoomEnteredTimepoint
        {
            State = new CombatRoomEnteredState
            {
                EnemyCount = enemyNames.Count,
                EnemyNames = enemyNames.ToArray()
            }
        };
    }
}
