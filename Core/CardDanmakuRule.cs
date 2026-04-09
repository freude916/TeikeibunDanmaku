namespace TeikeibunDanmaku.Core;

public sealed record CardDanmakuRule(
    string RuleId,
    IReadOnlyList<string> RequiredTags,
    IReadOnlyList<string> Messages
);
