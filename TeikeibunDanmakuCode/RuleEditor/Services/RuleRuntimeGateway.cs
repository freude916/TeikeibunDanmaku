using TeikeibunDanmaku.Core;

namespace TeikeibunDanmaku.RuleEditor.Services;

public sealed class RuleRuntimeGateway
{
    public void ReloadRulesFromFile(string filePath)
    {
        RuleRuntime.ReloadRulesFromFile(filePath);
    }
}
