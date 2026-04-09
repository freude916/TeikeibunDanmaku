# 自动气氛弹幕：后端检测与发送架构（第一版）

## 输入来源
- `.reference/公式化尖塔.xlsx`
- `../VoiceToPlay` 的时间点补丁
- `../STS2MCP` 的状态判定与房间/覆盖层识别
- `.reference/sts2src` 的核心事件与实体事件

## 1. 先看具体业务（不抽象）

### 1.1 现有时间点与业务触发（来自 `通用弹幕`）
- 当前表里有 `107` 条“条件+弹幕”记录。
- 分类（按“分类”列）出现频次：

| 分类     | 条数 | 已有具体业务例子                                          |
|--------|---:|---------------------------------------------------|
| 战斗出牌   | 19 | 单卡伤害>50、回合伤害/抽牌比超阈值、手牌同类型、牌序错误、单卡多段攻击30+、回合打牌100+ |
| 时机确定   | 16 | 丢瓶中精灵、卡组>25、删牌、感染牌>=10、偷牌不拿回、获得瓶中精灵、药水栏位>3        |
| 奖励     | 13 | 精英无伤、看见终端、拿到终端、跳过终端、同卡拿>=2次、关键终端二选一               |
| 角色专属   | 10 | 战士撕裂+疼痛、鸡煲拿关键遗物、猎人遇猎人杀手、开局选储君/鸡煲                  |
| 火堆     |  8 | 满血睡觉、低血还切肉刀、10血以下睡觉、boss前10血敲牌、有帐篷进火堆             |
| 战斗结算   |  8 | 保留药水通关、击杀最终boss后丢药水、离开boss关、3回合前解决怪、第一关前死亡        |
| 商店     |  7 | 离店金币>50%、单次购买花费>50%、看见终端、首购<=3秒、商店跳过终端            |
| 事件选项   |  7 | 选择低语耳环、断桥掉关键牌/诅咒、可删牌时删/不删、瓦库拿伞                    |
| 时机不确定  |  6 | 暂停、弹幕数>5、一段时间无弹幕、唐诗操作                             |
| 一次性    |  6 | 开局台词、死亡台词、强劲音乐、开局选角色                              |
| 战斗敌人   |  5 | 单次受伤>10、遇实验体无实体击杀、鸡煲低血、猎人遇大颚虫                     |
| 随机触发   |  4 | 开局随机台词、种子简单一路无波折                                  |
| mod    |  2 | 事件选项一样、同遗物进负面遗物事件                                 |
| 战斗条件   |  2 | 差1血存活/击杀、回合时长>30秒                                 |
| 进入战斗   |  1 | 进入boss关时播报当前血量                                    |
| 地图中    |  1 | 遇见负面问号事件                                          |
| 进入任意房间 |  1 | 进入任意房间时药水栏有果汁                                     |

### 1.2 现有“事物种类”清单（Excel 其它工作表）

#### 卡牌（`35` 条）
- 等级分布：
  - `独特机制` 11
  - `角色终端` 10
  - `流派终端` 4
  - `普通` 4
  - `笑嘻了` 2
  - `流派普通` 2
  - `无色终端` 2
- 已有明确业务词条（示例，不是抽象类名）：
  - `狂宴`、`原始力量`、`神化`、`灼热`、`感染`、`吹哨`、`蛇咬`、`回响形态`、`电流相生`、`恶魔形态`、`腐化`、`幽魂形态`、`壁垒`、`粒子墙`、`未掘宝石`、`至亮之焰`、`偏差认知`、`旋风斩`
- 现有可直接落地的卡牌相关条件语义：
  - “打出时”“获得时”“斩杀时”“变成零费时”“牌库没牌时”“被烧掉起作用时”

#### 遗物（`17` 条）
- 示例词条：`X药`、`红牛`、`哈钢`、`帐篷`、`铜制鳞片`、`战纹涂料`、`磨刀石`、`草莓`、`领主阳伞`、`图章戒指/古钱币`、`蛇眼`、`冻眼`
- 目前只有少量遗物有附加条件（如 `哈钢` 对应“防牌/风的女儿”）

#### 敌人（`16` 条）
- 强度分布：`精英` 7、`boss` 5、`超模小怪` 3、`小怪` 1
- 有条件敌人条目 `12` 条（例如）：
  - 墨影幻灵：卡组有高伤单卡
  - 女王：第二回合击杀
  - 旧日雕像：第三回合
  - 猎人杀手：过牌>4
  - 蟑螂群：铸剑流派/单卡伤害>16
  - 三骑士：幽灵骑士未败且回合结束有余手牌
  - 沙虫：被吞、或携带蜥蜴尾巴/瓶中精灵被吞

#### 事件（`7` 条）
- `掉血敲牌`、`轮盘抽到诅咒`、`闪耀之光`、`复制神坛`、`桥选到关键牌`、`茶艺大师`、`挑娃娃`

### 1.3 已出现的“阈值型条件”
- 血量阈值：`<20`、`<=10`、`差1血`
- 牌组规模：`>25`、`>256`
- 金币占比：购买价格 `>50%`、离店金币 `>50%`
- 时长阈值：`首购<=3秒`、`战斗>10min`、`回合>30秒`
- 回合/次数阈值：`第三回合`、`伤害次数>阈值`、`单卡攻击次数>=30`、`回合打牌>=100`
- 计数阈值：`感染牌>=10`、`弹幕数量>5`、`过牌>4`、`能力牌>3`

## 2. 可直接挂钩的检测时间点（参考现有代码）

### 2.1 核心运行/房间时间点（游戏源码）
- `RunManager`：
  - 事件定义：`RunStarted`、`RoomEntered`、`RoomExited`、`ActEntered`
  - 参考：`.reference/sts2src/sts2/MegaCrit.Sts2.Core.Runs/RunManager.cs:164`, `:166`, `:168`, `:170`
  - 事件触发：`:461`, `:858`, `:910`, `:1028`
- `CombatManager`：
  - 事件定义：`CombatSetUp`、`TurnStarted`、`TurnEnded`、`CombatWon`、`CombatEnded`、`PlayerEndedTurn`
  - 参考：`.reference/sts2src/sts2/MegaCrit.Sts2.Core.Combat/CombatManager.cs:141-155`
  - 触发点：`TurnStarted` `:378/:386`、`TurnEnded` `:988`、`CombatWon` `:689`、`CombatEnded` `:634/:693`

### 2.2 战斗内细粒度时间点（游戏源码）
- `CombatHistory`（最关键）：
  - 统一变更事件 `Changed`
  - 可读条目：`CardPlayStarted/Finished`、`CardDrawn`、`CardExhausted`、`CreatureAttacked`、`DamageReceived`、`PotionUsed`
  - 参考：`.reference/sts2src/sts2/MegaCrit.Sts2.Core.Combat.History/CombatHistory.cs:24`, `:32`, `:37`, `:52`, `:57`, `:67`, `:72`, `:97`
- `CardModel`：
  - 事件 `Played`、`Drawn`
  - 参考：`.reference/sts2src/sts2/MegaCrit.Sts2.Core.Models/CardModel.cs:847`, `:849`, `:1559`, `:1673`
- `CardPile`：
  - 事件 `CardAdded`、`CardRemoved`、`ContentsChanged`
  - 参考：`.reference/sts2src/sts2/MegaCrit.Sts2.Core.Entities.Cards/CardPile.cs:30-34`
- `Creature`：
  - 事件 `CurrentHpChanged`、`BlockChanged`、`Died`
  - 参考：`.reference/sts2src/sts2/MegaCrit.Sts2.Core.Entities.Creatures/Creature.cs:276`, `:278`, `:290`, `:440`
- `Player`：
  - 事件 `RelicObtained`、`RelicRemoved`、`PotionProcured`、`PotionDiscarded`、`UsedPotionRemoved`、`GoldChanged`
  - 参考：`.reference/sts2src/sts2/MegaCrit.Sts2.Core.Entities.Players/Player.cs:153-167`, `:357`, `:375`, `:499`, `:520`, `:527`

### 2.3 房间/UI 时间点（VoiceToPlay 已验证）
- 商店：
  - `NMerchantRoom._EnterTree` / `_ExitTree`
  - `NMerchantInventory.Open`
  - `MerchantEntry.InvokePurchaseCompleted`
  - 参考：`../VoiceToPlay/Commands/Shop/Patches/NMerchantRoomLifecyclePatches.cs:11`, `:30`, `:44`, `:57`
- 事件：
  - `NEventRoom.SetOptions`、`NEventLayout.DisableEventOptions`
  - 参考：`../VoiceToPlay/Commands/Events/Patches/NEventRoomLifecyclePatches.cs:10`, `:23`
- 奖励/选牌：
  - `NRewardsScreen.SetRewards`、`AfterOverlayShown`、`AfterOverlayClosed`
  - `NCardRewardSelectionScreen.AfterOverlayShown/Closed`
  - 参考：`../VoiceToPlay/Commands/Rewards/Patches/NRewardsScreenLifecyclePatches.cs:9`, `:19`, `:29` 与 `../VoiceToPlay/Commands/CardRow/Patches/NCardRewardSelectScreenLifecyclePatches.cs:9`, `:19`
- 火堆：`NRestSiteRoom._Ready`、`_ExitTree`
  - 参考：`../VoiceToPlay/Commands/RestSite/Patches/NRestSiteRoomLifecyclePatches.cs:10`, `:20`
- 地图：`NMapScreen` 构造后的 `Opened` / `Closed`
  - 参考：`../VoiceToPlay/Commands/Map/Patches/NMapScreenLifecyclePatches.cs:9`

### 2.4 轮询兜底时间点（STS2MCP 已验证）
- `BuildGameState` 对 `state_type` 的识别已经覆盖：
  - `monster/elite/boss/hand_select/event/map/shop/rest_site/treasure/rewards/card_reward/card_select/...`
  - 参考：`../STS2MCP/McpMod.StateBuilder.cs:65-208`
- 可直接借鉴为“状态变化触发器”：`prev_state_type -> current_state_type`

## 3. 后端架构（按当前业务落地）



## 4. JSONC 配置形态（按你要求：时间点+阈值+tag+弹幕）

### 4.1 规则文件 `danmaku_rules.jsonc`

```jsonc
{
  "rules": [
    {
      "id": "shop_buy_fast",
      "enabled": true,
      "timepoint": "shop.purchased",
      "thresholds": {
        "purchase_elapsed_seconds_le": 3,
        "price_ratio_ge": 0.5
      },
      "tag_filters": {
        "purchased_item_any": ["terminal"]
      },
      "messages": ["本能反应了", "贵有贵的道理"],
      "cooldown_ms": 120000,
      "once_per_run": false
    },
    {
      "id": "rest_low_hp_sleep",
      "enabled": true,
      "timepoint": "rest.opened",
      "thresholds": {
        "player_hp_le": 10,
        "rest_has_option_count_ge": 1
      },
      "messages": ["好好睡，好好睡", "这下不得不睡了"],
      "cooldown_ms": 60000,
      "once_per_run": false
    }
  ]
}
```

### 4.2 实体 Tag 文件 `danmaku_tags.jsonc`

```jsonc
{
  "cards": {
    "回响形态": ["terminal", "character_specific"],
    "电流相生": ["terminal", "character_specific"],
    "恶魔形态": ["terminal", "high_single_damage"],
    "腐化": ["terminal", "burn_related"],
    "感染": ["infection_related"],
    "旋风斩": ["x_cost", "multi_hit", "high_single_damage"]
  },
  "relics": {
    "帐篷": ["camp"],
    "图章戒指": ["economy"],
    "古钱币": ["economy"],
    "黄金印": ["economy"],
    "红牛": ["energy"]
  },
  "enemies": {
    "活雾": ["over_tuned"],
    "猎人杀手": ["over_tuned", "hunter_related"],
    "蟑螂群": ["over_tuned"],
    "沙虫": ["boss", "swallow_mechanic"]
  },
  "events": {
    "掉血敲牌": ["delete_card_event"],
    "挑娃娃": ["delete_card_event"],
    "桥选到关键牌": ["bridge_event"]
  }
}
```

### 4.3 可选默认阈值 `danmaku_thresholds.jsonc`

```jsonc
{
  "defaults": {
    "low_hp": 20,
    "critical_hp": 10,
    "deck_large": 25,
    "deck_huge": 256,
    "shop_expensive_ratio": 0.5,
    "purchase_elapsed_seconds": 3,
    "long_battle_seconds": 600,
    "long_turn_seconds": 30,
    "single_card_damage": 50,
    "single_card_hits": 30
  }
}
```

## 5. 第一阶段落地范围（建议）
- 先做 `20` 个 timepoint（上表全部），不做复杂 DSL。
- 先覆盖 Excel 里最密集的 5 组：
  - `战斗出牌`
  - `时机确定`
  - `奖励`
  - `商店`
  - `火堆`
- 条件只实现阈值比较与 tag 命中。
- 先不做“条件类型系统”和脚本化表达式。
