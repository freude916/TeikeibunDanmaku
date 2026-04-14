using TeikeibunDanmaku.Core;
using TeikeibunDanmaku.Core.Rules;
using TeikeibunDanmaku.Io;

namespace TeikeibunDanmaku.RuleEditor.Services;

public sealed class RuleFileGateway
{
    public IReadOnlyList<string> ListRuleFiles()
    {
        var dir = RuleRuntime.GetRulesDirectoryPath();
        Directory.CreateDirectory(dir);
        return RuleJsoncIo.ListJsoncFiles(dir);
    }

    public string EnsureFilePath(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var safeName = Path.GetFileName(fileName.Trim());
        if (string.IsNullOrWhiteSpace(safeName))
        {
            throw new InvalidOperationException("File name cannot be empty.");
        }

        if (!safeName.EndsWith(".jsonc", StringComparison.OrdinalIgnoreCase))
        {
            safeName += ".jsonc";
        }

        var dir = RuleRuntime.GetRulesDirectoryPath();
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, safeName);
    }

    public IReadOnlyList<RuleDto> LoadDtos(string filePath)
    {
        return RuleJsoncIo.ImportDtosFromFile(filePath);
    }

    public void SaveDtos(string filePath, IReadOnlyList<RuleDto> rules)
    {
        RuleJsoncIo.ExportDtosToFile(filePath, rules);
    }
}
