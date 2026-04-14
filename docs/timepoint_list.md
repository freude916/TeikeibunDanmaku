# Timepoint 列表

本文基于 `TeikeibunDanmakuCode/Timepoints/TimepointStateResolver.cs` 当前注册内容整理。

## 长期字段（LSFR）

来源：`TeikeibunDanmakuCode/Core/Blackboard/LongStateFieldRegistry.cs`。

| 外部 Key               | 类型    | DataField 显示名 |
|----------------------|-------|---------------|
| `run.player_hp`      | `int` | 玩家血量          |
| `run.player_gold`    | `int` | 玩家金币          |
| `run.player_deck_size` | `int` | 牌组数量          |
| `run.player_deck` | `IReadOnlyList<string>` | 牌组列表 |
| `run.player_deck_archetype_table` | `IReadOnlyDictionary<string, int>` | 牌组流派表 |
| `run.player_relics` | `IReadOnlyList<string>` | 遗物列表 |

## 总览

| Timepoint ID          | 显示名    | 对应 State                 |
|-----------------------|--------|--------------------------|
| `run.started`         | 开始游戏   | `RunStartedState`        |
| `reward.seen`         | 卡牌奖励出现 | `CardState`              |
| `reward.open`         | 卡牌奖励打开 | `CardPoolOpenState`      |
| `shop.seen`           | 商店卡牌出现 | `CardState`              |
| `shop.open`           | 商店打开   | `CardPoolOpenState`      |
| `event.seen`          | 事件出现   | `EventState`             |
| `combat.card_played`  | 战斗出牌完成 | `CardPlayState`          |
| `combat.room_entered` | 进入战斗房间 | `CombatRoomEnteredState` |

## State 字段

### RunStartedState（`run.started`）

| 字段名              | 类型       | DataField 显示名 |
|------------------|----------|---------------|
| `CharacterName`  | `string` | 角色名           |
| `AscensionLevel` | `int`    | 进阶            |

### CardState（`reward.seen`、`shop.seen`）

| 字段名            | 类型       | DataField 显示名 |
|----------------|----------|---------------|
| `ModelId`      | `string` | 卡牌ID          |
| `Title`        | `string` | 卡牌名           |
| `CostsX`       | `bool`   | X费            |
| `EnergyCost`   | `int`    | 能量消耗          |
| `EnergyEarned` | `int`    | 获得能量          |
| `HpLoss`       | `int`    | 生命损失          |
| `Strength`     | `int`    | 力量            |
| `Dexterity`    | `int`    | 敏捷            |
| `Damage`       | `int`    | 伤害            |
| `Block`        | `int`    | 格挡            |
| `Repeat`       | `int`    | 多段            |
| `IsTerminal`   | `bool`   | 是否终端          |
| `Archetypes`   | `IReadOnlyList<string>` | 流派 |

### CardPoolOpenState（`reward.open`、`shop.open`）

| 字段名            | 类型                      | DataField 显示名 |
|----------------|-------------------------|---------------|
| `TerminalCount` | `int`                  | 终端数量          |
| `Archetypes`    | `IReadOnlyList<string>` | 流派列表          |

### EventState（`event.seen`）

| 字段名         | 类型       | DataField 显示名 |
|-------------|----------|---------------|
| `EventId`   | `string` | 事件ID          |
| `EventName` | `string` | 事件名           |

### CardPlayState（`combat.card_played`）

| 字段名                    | 类型       | DataField 显示名 |
|------------------------|----------|---------------|
| `ModelId`              | `string` | 卡牌ID          |
| `Title`                | `string` | 卡牌名           |
| `CostsX`               | `bool`   | X费            |
| `EnergyCost`           | `int`    | 能量消耗          |
| `DamageTotal`          | `int`    | 总伤害           |
| `DamageUnblocked`      | `int`    | 未格挡伤害         |
| `DamageBlocked`        | `int`    | 已格挡伤害         |
| `DamageHits`           | `int`    | 伤害段数          |
| `DamageMaxSingle`      | `int`    | 单段最大伤害        |
| `DamageEnemyUnblocked` | `int`    | 对敌未格挡伤害       |
| `HasEnemyDamage`       | `bool`   | 是否造成敌方伤害      |

### CombatRoomEnteredState（`combat.room_entered`）

| 字段名          | 类型                      | DataField 显示名 |
|--------------|-------------------------|---------------|
| `EnemyCount` | `int`                   | 敌人数量          |
| `EnemyNames` | `IReadOnlyList<string>` | 敌人名称列表        |
