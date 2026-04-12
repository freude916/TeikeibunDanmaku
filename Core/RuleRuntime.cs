namespace TeikeibunDanmaku.Core.Rules;

public static class RuleRuntime
{
    private static RuleEngine? _ruleEngine;
    private static IReadOnlyList<Rule> _configuredRules = [];

    public static void Initialize()
    {
        if (_ruleEngine != null)
        {
            return;
        }

        _ruleEngine = new RuleEngine(_configuredRules);
    }

    public static void ConfigureRules(IReadOnlyList<Rule> rules)
    {
        _configuredRules = rules ?? throw new ArgumentNullException(nameof(rules));
        
        if (_ruleEngine == null)
        {
            return;
        }
        
        _ruleEngine.Dispose();
        _ruleEngine = new RuleEngine(_configuredRules);
        MainFile.Logger.Info($"Initalized RuleRuntime with {rules.Count} rules.");
    }
}
