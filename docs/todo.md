# TODO：通用弹幕条件补齐清单（最小可用）

目标：优先补齐 `02_通用弹幕.csv` 中高频条件所需能力，只看条件结构，不看文案内容。

## P0：先补（覆盖面最大）

### 1. Timepoint
- [ ] `shop.purchased`
  - 覆盖：商店单次购买价格阈值、首购时机、终端跳过相关后续判断
- [ ] `shop.left`（或 `shop.closed`）
  - 覆盖：离店金币占比、商店“看见但没拿”类
- [ ] `reward.picked`
  - 覆盖：拿到终端、同卡抓取次数、同流派终端累计
- [ ] `reward.skipped`
  - 覆盖：跳过终端
- [ ] `event.option_selected`
  - 覆盖：事件选项分支（删牌/不删牌/拿伞等）
- [ ] `rest.option_selected`
  - 覆盖：火堆具体选择（睡觉/敲牌/烹饪）
- [ ] `combat.damage_received`
  - 覆盖：单次受伤阈值、差一滴血存活

### 2. Condition
- [ ] 注册 `not`（当前类型常量存在，但未在 registry 注册）
- [ ] 增加 `gte` / `lte`
  - 覆盖：`>=` / `<=` / “至少” / “以下”语义
- [ ] 增加“规则级门控”能力
  - `once_per_run`
  - `cooldown_ms`
  - 覆盖：一次性、降频、时机不确定类防刷屏

### 3. 运行时字段（Long/Cache）
- [ ] `run.floor`、`run.act`
- [ ] `run.room_type`
- [ ] `run.potion_slot_count`
- [ ] `combat.turn`
- [ ] `combat.elapsed_ms`
- [ ] `combat.cards_played_this_turn`
- [ ] `combat.cards_drawn_this_turn`
- [ ] `combat.damage_taken_this_turn`

## P1：第二批（结构能力）

### 4. 条件表达扩展
- [ ] 比值条件：`ratio_gt`（例如 `A / B > x`）
  - 覆盖：回合伤害/抽牌比
- [ ] 时序条件：`sequence` / `ordered_pair`
  - 覆盖：先后顺序类（先打伤害再打易伤）
- [ ] 窗口条件：`within_ms` / `no_event_within_ms`
  - 覆盖：一秒内不拿、首购 3 秒内
- [ ] 次数条件：`count_in_window_gte`
  - 覆盖：连续 N 次、短时间高频触发

### 5. 事件负载补充
- [ ] `shop.purchased` 增加：`item_id`、`item_price`、`is_first_purchase_in_shop`
- [ ] `reward.picked` 增加：`card_id`、`archetypes`
- [ ] `event.option_selected` 增加：`event_id`、`option_id`、`option_text`
- [ ] `rest.option_selected` 增加：`option_type`

## P2：后续（长尾）

### 6. Timepoint
- [ ] `combat.turn_started`
- [ ] `combat.turn_ended`
- [ ] `combat.won`
- [ ] `combat.lost`
- [ ] `run.ended`（胜利/死亡/全军覆没）
- [ ] `room.entered`（任意房间统一入口）
- [ ] `map.node_entered`（地图节点）

### 7. 规则引擎
- [ ] 支持“状态迁移触发”（A -> B）
- [ ] 支持“去重键”防重复播报（同条件短时重复命中）
- [ ] 增加调试输出：命中规则、命中字段快照

## 验收标准（最低）
- [ ] 可表达并稳定触发：`>=`、`<=`、`NOT`
- [ ] 可覆盖：商店购买、奖励拿/跳、事件选项、火堆选择、单次受伤
- [ ] 支持 `once_per_run` 和 `cooldown_ms`
- [ ] `dotcheck.sh` 通过且 RuleEditor 可选新 timepoint / 新 condition
