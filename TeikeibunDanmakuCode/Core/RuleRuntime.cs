using TeikeibunDanmaku.Core.Rules;
using TeikeibunDanmaku.Io;
using TeikeibunDanmaku.Timepoints;
using TeikeibunDanmaku.Utils;

namespace TeikeibunDanmaku.Core;

public static class RuleRuntime
{
    private static RuleEngine? _ruleEngine;
    private static readonly List<Rule> ConfiguredRules = [];
    private static readonly TimepointStateResolver StateResolver = new TimepointStateResolver();
    private static readonly string RulesDirectoryPath = Path.Combine(ModPathResolver.ResolveModDirectory(), "rules");
    public static string GetRulesDirectoryPath() => RulesDirectoryPath;

    public static void Initialize()
    {
        if (_ruleEngine != null)
        {
            return;
        }

        _ruleEngine = new RuleEngine(ConfiguredRules);
        MainFile.Logger.Info($"Initialized RuleRuntime with {ConfiguredRules.Count} rules.");
    }

    public static void ConfigureRules(IReadOnlyList<Rule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ConfiguredRules.Clear();
        ConfiguredRules.AddRange(rules);
        
        if (_ruleEngine == null)
        {
            return;
        }
        
        _ruleEngine.Dispose();
        _ruleEngine = new RuleEngine(ConfiguredRules);
        MainFile.Logger.Info($"Initialized RuleRuntime with {rules.Count} rules.");
    }

    public static void AppendRules(IReadOnlyList<Rule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        if (rules.Count == 0)
        {
            return;
        }

        ConfiguredRules.AddRange(rules);
        _ruleEngine?.AppendRules(rules);
        MainFile.Logger.Info($"Appended {rules.Count} rules. Total rules: {ConfiguredRules.Count}.");
    }

    public static IReadOnlyList<Rule> LoadRulesFromDefaultDirectory()
    {
        return RuleJsoncIo.ImportFromDirectory(RulesDirectoryPath, StateResolver);
    }

    public static IReadOnlyList<Rule> LoadRulesFromFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        return RuleJsoncIo.ImportFromFile(filePath, StateResolver);
    }

    public static void ReloadRulesFromFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        var rules = LoadRulesFromFile(filePath);
        ConfigureRules(rules);
        MainFile.Logger.Info($"Reloaded {rules.Count} rules from '{filePath}'.");
    }

    public static void ExportConfiguredRulesToDefaultFile(string fileName = "rules.export.jsonc")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        var outputPath = Path.Combine(RulesDirectoryPath, fileName);
        RuleJsoncIo.ExportToFile(outputPath, ConfiguredRules);
        MainFile.Logger.Info($"Exported {ConfiguredRules.Count} rules to '{outputPath}'.");
    }
}
