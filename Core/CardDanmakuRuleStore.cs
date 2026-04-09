namespace TeikeibunDanmaku.Core;

public static class CardDanmakuRuleStore
{
    // MVP: 先用常量硬编码规则，后续替换为 jsonc 解析结果。
    private static readonly CardDanmakuRule[] CombatCardPlayedRules =
    [
        new(
            "combat_card_strike",
            ["timepoint.combat.card_played", "card.tag.strike"],
            ["我说打击特别强"]
        ),
        new(
            "combat_card_defend",
            ["timepoint.combat.card_played", "card.tag.defend"],
            ["有攻有防，好牌多抓"]
        ),
        new(
            "combat_card_type_power",
            ["timepoint.combat.card_played", "card.type.power"],
            ["能力牌启动了"]
        ),
        new(
            "combat_card_type_attack",
            ["timepoint.combat.card_played", "card.type.attack"],
            ["攻击牌，启动！"]
        ),
        new(
            "combat_card_type_skill",
            ["timepoint.combat.card_played", "card.type.skill"],
            ["技能牌，启动！"]
        )
    ];

    public static IReadOnlyList<CardDanmakuRule> GetCombatCardPlayedRules()
    {
        return CombatCardPlayedRules;
    }
}
