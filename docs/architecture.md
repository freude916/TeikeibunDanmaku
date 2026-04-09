# 自动弹幕预制菜

高内聚：每个时间点收集一系列上下文并向 Engine 上报。 例如篝火负责收集选项数量？

为了可维护性，我们最好是每个类用一个方法读取上下文，尽管 Engine 很可能需要全量刷新。

```json lines
{
  "rule_id": "rest_low_hp_sleep",
  "timepoint": "rest.opened",
  "condition": {
    "type": "OR",
    "conditions": [
      {
        "type": "le",
        "property": "player_hp",
        "value": 20
      },
      {
        "type": "contain_id",
        "property": "player_deck",
        "ModelId": "PERFECT_STRIKE"
      },
      {
        "type": "contain_tag",
        "property": "player_deck",
        "ModelId": "STRIKE"
      },
      {
        "type": "is_id",
        "property": "recent_card",
        "ModelId": "BIRD_EGG"
      },
    ]
  },
  "messages": [
    "好好睡，好好睡",
    "这下不得不睡了"
  ],
  "cooldown_ms": 60000,
  "once_per_run": false
}
```

允许一个 rule_id 定义多次，可以合并 messages 作为弹幕拓展包，（甚至可能考虑合并 condition 的 OR）

类型不安全？ property 名字可能需要一个检查？

## 3.1 目录与职责（后端独立，不跨 UI 层）

建议新增：

```text
已删除
```

这样做的边界是：

- 可以分散的是“检测实现”（每个时间点一小块，像 VoiceToPlay）。
- 不能分散的是“规则判断与派发”（必须在 `Engine` 统一），否则会变散装逻辑。

## 3.2 首批时间点 ID（按业务优先级）

| timepoint_id               | 检测来源                                                   | 对应业务                   |
|----------------------------|--------------------------------------------------------|------------------------|
| `run.started`              | `RunManager.RunStarted`                                | 开局/开局角色台词              |
| `run.act_entered`          | `RunManager.ActEntered`                                | 进新幕/进 boss 前后          |
| `room.entered`             | `RunManager.RoomEntered` + 当前房间类型                      | 进入任意房间、进入商店、进火堆、进 boss |
| `room.exited`              | `RunManager.RoomExited` + 上一房间类型                       | 离开商店、离开 boss 房         |
| `shop.opened`              | `NMerchantRoom._EnterTree` / `NMerchantInventory.Open` | 商店看见终端、购买计时起点          |
| `shop.purchased`           | `MerchantEntry.InvokePurchaseCompleted`                | 购买<=3秒、单件花费比例、商店相关梗    |
| `event.options_ready`      | `NEventRoom.SetOptions`                                | 事件选项条件                 |
| `event.options_closed`     | `NEventLayout.DisableEventOptions`                     | 事件结束收口                 |
| `rest.opened`              | `RestSiteSynchronizer.BeginRestSite`                   | 火堆选项数、有帐篷进火堆           |
| `rest.option_chosen`       | `RestSiteSynchronizer.AfterPlayerOptionChosen`         | 低血睡觉、满血睡觉、切肉刀烹饪       |
| `rewards.opened`           | `NRewardsScreen.SetRewards`                            | 看见终端、跳过终端              |
| `card_reward.opened`       | `NCardRewardSelectionScreen.AfterOverlayShown`         | 抓牌相关条件                 |
| `combat.started`           | `CombatManager.CombatSetUp`                            | 进入战斗/精英/boss           |
| `combat.turn_started`      | `CombatManager.TurnStarted`                            | 回合开始统计重置               |
| `combat.card_played`       | `CardModel.Played` + `CombatHistory`                   | 单卡伤害阈值、多段攻击、连打         |
| `combat.turn_ended`        | `CombatManager.TurnEnded`                              | 回合剩能量+手牌、回合时长          |
| `combat.ended`             | `CombatManager.CombatWon/CombatEnded`                  | 无伤、回合数、战斗结算            |
| `player.hp_changed`        | `Creature.CurrentHpChanged`(玩家)                        | 低血阈值、差1血               |
| `player.died`              | `Creature.Died`(玩家)                                    | 死亡一次性台词                |
| `deck.changed`             | `CardPile.CardAdded/CardRemoved`(Deck)                 | 卡组>25、感染牌>=10、重复抓牌     |
| `player.inventory_changed` | `Relic/Potion/Gold` 事件                                 | 获得瓶中精灵、药水槽位、金币占比       |

## 3.4 规则命中流程

1. `Detection` 触发 `timepoint_id`
2. 只取同 `timepoint_id` 的规则
3. `DanmakuStore` 在该时刻动态读取运行态（`RunManager/CombatManager/Player/...`）
4. 辅助类单例 `ConditionEvaluator` 的 static 方法逐条比较 `condition` 与 tag 条件，得到 `ConditionResult`
5. 仅当 `ConditionResult.IsMatch = true` 时，`rule_id` 进入 `DanmakuSender` `cooldown` + `dedupe` + `once_per_run` 闸门
6. 进入发送队列并选 1 条弹幕文本
7. 命中与未命中都可记录 `reasons`（用于 debug 与回放）

---

## 4. 三层分离架构：钩子-条件-发送器

### 4.1 核心洞见

规则命中流程勾勒出三层分离：

```
Detection(钩子) → DanmakuStore(规则) → ConditionEvaluator(条件) → DanmakuSender(发送)
```

### 4.2 钩子层（Detection）- "触须"

**职责**：监听游戏事件，转换为 `timepoint_id`

钩子是分散的，像散落在游戏各处的"触须"，每个 `timepoint_id` 一个 Detection 类：

```csharp
// 钩子贴近游戏事件源
[HarmonyPatch(typeof(NRestSiteRoom), nameof(NRestSiteRoom._Ready))]
class RestOpenedDetection {
    static void Postfix() => DanmakuEngine.Instance.OnTimepoint("rest.opened");
}
```

从弹幕表格的条件分类来看，需要覆盖的钩子点：

| 弹幕表格分类   | 对应 timepoint                 | 数量    |
|----------|------------------------------|-------|
| 火堆       | `rest.opened`                | 8 条   |
| 商店       | `shop.*`                     | 7 条   |
| 奖励       | `rewards.*`, `card_reward.*` | 10+ 条 |
| 战斗出牌     | `combat.card_played`         | 15+ 条 |
| 战斗敌人     | `combat.started` + 敌人检测      | 5+ 条  |
| 战斗结算     | `combat.ended`               | 6 条   |
| 事件选项     | `event.options_ready`        | 8 条   |
| 时机确定/不确定 | 需要轮询或状态累积                    | 10+ 条 |

### 4.3 条件层（ConditionEvaluator）- "大脑"

**职责**：纯逻辑判断，不依赖游戏直接调用

`ConditionEvaluator` 应该是**无状态**的纯函数：

```csharp
static class ConditionEvaluator {
    public static bool Evaluate(ConditionNode condition, DanmakuContext ctx);
}
```

上下文 `DanmakuContext` 由 Engine 在触发时**一次性收集**，而不是让条件去主动查询游戏状态。

### 4.4 发送层（DanmakuSender）- "嘴巴"

**职责**：闸门控制（cooldown/dedupe/once_per_run）+ UI 展示

发送器负责：

- 随机选一条弹幕
- 应用 cooldown
- 去重（同一 rule_id 短时间内不重复）
- once_per_run 标记

---

## 5. 建议的项目结构

```
TeikeibunDanmaku/
├── Detection/           # 钩子层 - 散落各处的 Harmony Patch
│   ├── RunDetection.cs
│   ├── CombatDetection.cs
│   ├── RestDetection.cs
│   └── ShopDetection.cs
│
├── Core/                # 后端核心
│   ├── DanmakuEngine.cs       # 入口，协调三层
│   ├── DanmakuContext.cs      # 上下文快照
│   ├── ConditionEvaluator.cs  # 条件评估（纯函数）
│   └── DanmakuSender.cs       # 发送器
│
├── Rules/               # 规则定义
│   ├── DanmakuRule.cs        # 规则模型
│   └── DanmakuRuleStore.cs   # 规则加载/查询
│
└── Data/                # 规则数据文件
    └── rules/*.json          # 从表格翻译来的规则
```

**边界清晰**：

- `Detection/` 可以散落，只管触发
- `Core/` 必须集中，Engine 是唯一的协调中心
- `Rules/` 和 `Data/` 是配置层，不写逻辑

---

## 6. 上下文快照设计（DanmakuContext）

从表格条件归纳，按**调用频率**和**来源**分组：

### 6.1 玩家基础（全局可用）

| 字段                    | 类型             | 来源                   | 使用场景             |
|-----------------------|----------------|----------------------|------------------|
| `player_character`    | enum           | `RunManager`         | 角色专属弹幕（储君/猎人/鸡煲） |
| `player_hp`           | int            | `Player.CurrentHp`   | 低血量、差1血          |
| `player_max_hp`       | int            | `Player.MaxHp`       | 满血睡觉             |
| `player_gold`         | int            | `Player.Gold`        | 金币占比             |
| `player_relics`       | `List<string>` | `Player.Relics`      | 有帐篷、有瓶中精灵        |
| `player_potions`      | `List<string>` | `Player.Potions`     | 药水栏有果汁           |
| `player_potion_slots` | int            | `Player.PotionSlots` | 药水槽位>3           |

### 6.2 牌组（全局可用，变化时更新）

| 字段                    | 类型                        | 来源                  | 使用场景       |
|-----------------------|---------------------------|---------------------|------------|
| `deck_size`           | int                       | `Deck.Count`        | 牌组>25、>256 |
| `deck_card_ids`       | `List<string>`            | `Deck.GetCardIds()` | 拥有某张卡      |
| `deck_card_tags`      | `Dictionary<string, int>` | 遍历计算                | 打击/防御数量    |
| `grab_count_this_run` | `Dictionary<string, int>` | 累积统计                | 重复抓牌       |

### 6.3 战斗（combat.* 触发时可用）

| 字段                         | 类型                 | 来源                      | 使用场景         |
|----------------------------|--------------------|-------------------------|--------------|
| `combat_enemies`           | `List<string>`     | `CombatManager.Enemies` | 猎人遇大颚虫、储君遇蟑螂 |
| `combat_turn`              | int                | `CombatManager.Turn`    | X回合解决        |
| `combat_damage_taken`      | int                | 累积统计                    | 无伤           |
| `combat_total_damage`      | int                | 累积统计                    | 伤害阈值         |
| `combat_time_seconds`      | float              | 计时器                     | 战斗>10min     |
| `cards_played_this_turn`   | int                | 累积统计                    | 一回合100张      |
| `cards_played_this_combat` | `List<PlayRecord>` | 累积记录                    | 牌序判断         |

### 6.4 打牌（combat.card_played 触发时可用）

| 字段            | 类型             | 来源             | 使用场景                     |
|---------------|----------------|----------------|--------------------------|
| `card_id`     | string         | `Card.ModelId` | 打出特定卡                    |
| `card_tags`   | `List<string>` | `Card.Tags`    | 打出打击/防御                  |
| `card_damage` | int            | 计算结果           | 单卡伤害>50                  |
| `card_hits`   | int            | `Card.Hits`    | 多段攻击                     |
| `card_source` | enum           | 来源标记           | 塔2获得一代卡 （这不对，一代卡是一种 tag） |

### 6.5 房间/商店/火堆/事件（特定触发时可用）

| 字段                       | 类型               | 触发点                   | 使用场景       |
|--------------------------|------------------|-----------------------|------------|
| `room_type`              | enum             | `room.entered`        | 商店、火堆、boss |
| `is_before_boss`         | bool             | `room.entered`        | boss前火堆    |
| `shop_items`             | `List<ShopItem>` | `shop.opened`         | 看见终端、价格    |
| `shop_time_to_first_buy` | float            | `shop.purchased`      | 3秒内购买      |
| `reward_terminals`       | `List<string>`   | `rewards.opened`      | 看见终端、跳过    |
| `reward_skipped`         | bool             | `rewards.*`           | 跳过终端       |
| `rest_options`           | `List<string>`   | `rest.opened`         | 睡觉、敲牌、切肉刀  |
| `rest_action`            | string           | 用户选择后                 | 睡觉/敲牌      |
| `event_id`               | string           | `event.options_ready` | 特定事件       |
| `event_options`          | `List<string>`   | `event.options_ready` | 选项一样       |

### 6.6 运行态（全局可用）

| 字段                   | 类型    | 来源               | 使用场景 |
|----------------------|-------|------------------|------|
| `run_act`            | int   | `RunManager.Act` | 进新幕  |
| `run_time_seconds`   | float | 计时器              | 农种判断 |
| `danmaku_queue_size` | int   | Engine内部         | 弹幕过多 |

---

## 7. 上下文收集策略

### 7.1 按需收集 vs 全量刷新

```
玩家基础：懒加载，首次访问时读取，变化时失效
牌组：    懒加载 + deck.changed 事件失效
战斗：    combat.started 时清零，combat.* 时累积
打牌：    combat.card_played 时构造一次性快照
房间类：  对应 timepoint 触发时收集
```

### 7.2 收集时机

| timepoint             | 需要收集的上下文                 |
|-----------------------|--------------------------|
| `rest.opened`         | rest_options             |
| `rest.action`         | rest_options             |
| `shop.opened`         | shop_items               |
| `shop.purchased`      | shop_time_to_first_buy   |
| `rewards.opened`      | reward_terminals         |
| `card_reward.opened`  | grab_count_this_run      |
| `combat.started`      | combat_enemies           |
| `combat.card_played`  | 打牌快照                     |
| `combat.ended`        | 战斗统计                     |
| `event.options_ready` | event_id + event_options |

容易刷新的玩家牌组或遗物等干脆就在 Getter 中获取，或者在缓存时获取？

---

## 8. 条件表达式语法（Condition DSL）

### 8.1 基础结构

所有条件都是 JSON 对象，包含 `type` 字段指示运算类型：

```json lines
{
  "type": "<operator>",
} // other
```

### 8.2 数值比较（property 为 int/float）

```json lines
// player_hp <= 20
{
  "type": "le",
  "property": "player_hp",
  "value": 20
}

// deck_size > 256
{
  "type": "gt",
  "property": "deck_size",
  "value": 256
}

// player_hp == player_max_hp（满血）
{
  "type": "eq",
  "property": "player_hp",
  "value_from": "player_max_hp"
}
```

**支持运算符**：`eq` | `ne` | `lt` | `le` | `gt` | `ge`

**value vs value_from**：

- `value`: 字面量常量
- `value_from`: 引用上下文中的另一个字段（用于动态比较）

### 8.3 枚举匹配（property 为 enum/string）

```json lines
// 角色是战士
{
  "type": "is",
  "property": "player_character",
  "value": "IRONCLAD"
}

// 房间是商店
{
  "type": "is",
  "property": "room_type",
  "value": "SHOP"
}

// 火堆动作是睡觉
{
  "type": "is",
  "property": "rest_action",
  "value": "SLEEP"
}
```

**枚举值约定**：使用游戏内部常量的大写形式（如 `IRONCLAD`, `DEFECT`, `WATCHER`），即 Model.Id / EntryId

### 8.4 列表包含（property 为 List<string>）

```json lines
// 遗物列表包含"帐篷"
{
  "type": "contains",
  "property": "player_relics",
  "value": "TENT"
}

// 牌组包含特定卡牌
{
  "type": "contains",
  "property": "deck_card_ids",
  "value": "PERFECT_STRIKE"
}

// 敌人列表包含大颚虫
{
  "type": "contains",
  "property": "combat_enemies",
  "value": "JAW_WORM"
}
```

### 8.5 列表计数

```json lines
// 牌组大小 >= 25
{
  "type": "count_ge",
  "property": "deck_card_ids",
  "value": 25
}

// 火堆选项数 >= 3
{
  "type": "count_ge",
  "property": "rest_options",
  "value": 3
}

// 奖励终端数 == 2
{
  "type": "count_eq",
  "property": "reward_terminals",
  "value": 2
}
```

**支持运算符**：`count_eq` | `count_ne` | `count_lt` | `count_le` | `count_gt` | `count_ge`

### 8.6 字典查询（property 为 Dictionary<K,V>）

```json lines
// 牌组中打击牌数量 >= 5
{
  "type": "dict_ge",
  "property": "deck_card_tags",
  "key": "STRIKE",
  "value": 5
}

// 某张卡抓了至少 2 次
{
  "type": "dict_ge",
  "property": "grab_count_this_run",
  "key": "PERFECT_STRIKE",
  "value": 2
}
```

### 8.7 布尔检查

```json lines
// 是 boss 前的火堆
{
  "type": "is_true",
  "property": "is_before_boss"
}

// 不是无伤
{
  "type": "is_false",
  "property": "combat_no_damage"
}
```

### 8.8 组合条件

```json lines
// AND：所有条件都满足
{
  "type": "AND",
  "conditions": [
    {
      "type": "le",
      "property": "player_hp",
      "value": 10
    },
    {
      "type": "is",
      "property": "rest_action",
      "value": "SLEEP"
    }
  ]
}

// OR：任一条件满足
{
  "type": "OR",
  "conditions": [
    {
      "type": "le",
      "property": "player_hp",
      "value": 20
    },
    {
      "type": "contains",
      "property": "deck_card_ids",
      "value": "PERFECT_STRIKE"
    }
  ]
}

// NOT：取反
{
  "type": "NOT",
  "condition": {
    "type": "contains",
    "property": "player_relics",
    "value": "TENT"
  }
}
```

**嵌套示例**：牌组 > 256 且选择了低语耳环

```json lines
{
  "type": "AND",
  "conditions": [
    {
      "type": "gt",
      "property": "deck_size",
      "value": 256
    },
    {
      "type": "contains",
      "property": "player_relics",
      "value": "WHISPERING_EARRING"
    }
  ]
}
```

---

## 9. 条件表达式速查表

| 运算类型 | type                          | property 类型    | 示例                             |
|------|-------------------------------|----------------|--------------------------------|
| 数值比较 | `eq`/`ne`/`lt`/`le`/`gt`/`ge` | int/float      | `player_hp <= 20`              |
| 枚举匹配 | `is`                          | enum/string    | `player_character == IRONCLAD` |
| 列表包含 | `contains`                    | `List<string>` | 遗物含 TENT                       |
| 列表计数 | `count_ge` 等                  | `List<T>`      | 牌组 >= 25                       |
| 字典查询 | `dict_ge` 等                   | `Dict<K,V>`    | 打击牌 >= 5                       |
| 布尔检查 | `is_true`/`is_false`          | bool           | 是 boss 前                       |
| 组合   | `AND`/`OR`/`NOT`              | -              | 条件嵌套                           |

---

## 10. 待办：表格条件翻译示例

| 表格条件            | JSON 表达                                                                                                                                                                             |
|-----------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 火堆处选择满血睡觉       | `{ "type": "AND", "conditions": [{ "type": "eq", "property": "player_hp", "value_from": "player_max_hp" }, { "type": "is", "property": "rest_action", "value": "SLEEP" }] }`        |
| 牌组大于256且选择了低语耳环 | `{ "type": "AND", "conditions": [{ "type": "gt", "property": "deck_size", "value": 256 }, { "type": "contains", "property": "player_relics", "value": "WHISPERING_EARRING" }] }`    |
| 10滴血以下火堆睡觉      | `{ "type": "AND", "conditions": [{ "type": "le", "property": "player_hp", "value": 10 }, { "type": "is", "property": "rest_action", "value": "SLEEP" }] }`                          |
| 有帐篷且火堆选项>=3     | `{ "type": "AND", "conditions": [{ "type": "contains", "property": "player_relics", "value": "TENT" }, { "type": "count_ge", "property": "rest_options", "value": 3 }] }`           |
| 猎人遇见大颚虫         | `{ "type": "AND", "conditions": [{ "type": "is", "property": "player_character", "value": "SILENT" }, { "type": "contains", "property": "combat_enemies", "value": "JAW_WORM" }] }` |

