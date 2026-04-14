namespace TeikeibunDanmaku.Core.Condition;

public static class ConditionType
{
    public const string CondAnd = "and";
    public const string CondOr = "or";
    public const string Not = "not";
    
    public const string Eq = "eq";
    public const string ValueLt = "lt";
    public const string ValueGt = "gt";
    public const string StrFind = "find";
    public const string ListContains = "contains";

    private static readonly IReadOnlyDictionary<string, string> DisplayNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [CondAnd] = "条件与",
        [CondOr] = "条件或",
        [Eq] = "等于",
        [ValueLt] = "数值小于",
        [ValueGt] = "数值大于",
        [StrFind] = "字符串包含",
        [ListContains] = "列表包含"
    };

    public static string GetDisplayName(string type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return string.Empty;
        }

        return DisplayNames.TryGetValue(type, out var name) ? name : type;
    }

    public static bool HasDisplayName(string type)
    {
        return !string.IsNullOrWhiteSpace(type) && DisplayNames.ContainsKey(type);
    }

    public static IReadOnlyList<string> ListDisplayNameTypes()
    {
        return DisplayNames.Keys
            .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
