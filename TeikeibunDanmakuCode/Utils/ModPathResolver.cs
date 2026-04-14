using System.Reflection;

namespace TeikeibunDanmaku.Utils;

public static class ModPathResolver
{
    private static string? _cachedModDirectory;

    public static string ResolveModDirectory()
    {
        if (!string.IsNullOrWhiteSpace(_cachedModDirectory))
        {
            return _cachedModDirectory;
        }

        var modName = Assembly.GetExecutingAssembly().GetName().Name ?? "TeikeibunDanmaku";
        var processDir = Path.GetDirectoryName(Environment.ProcessPath);
        var candidates = new List<string>();

        if (!string.IsNullOrWhiteSpace(processDir))
        {
            candidates.Add(Path.Combine(processDir, "mods", modName));
        }

        candidates.Add(Path.Combine(AppContext.BaseDirectory, "mods", modName));
        candidates.Add(AppContext.BaseDirectory);

        if (!string.IsNullOrWhiteSpace(processDir))
        {
            candidates.Add(processDir);
        }

        candidates.Add(Environment.CurrentDirectory);

        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            var fullPath = Path.GetFullPath(candidate);
            if (!Directory.Exists(fullPath))
            {
                continue;
            }

            var hasManifest = File.Exists(Path.Combine(fullPath, $"{modName}.json"));
            var hasAssembly = File.Exists(Path.Combine(fullPath, $"{modName}.dll"));
            if (hasManifest || hasAssembly)
            {
                _cachedModDirectory = fullPath;
                return fullPath;
            }
        }

        _cachedModDirectory = AppContext.BaseDirectory;
        return _cachedModDirectory;
    }
}
