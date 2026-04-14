namespace TeikeibunDanmaku.Core.Blackboard;

[AttributeUsage(AttributeTargets.Property)]
public sealed class DataFieldAttribute: Attribute
{
    public DataFieldAttribute(string displayName)
    {
        DisplayName = string.IsNullOrWhiteSpace(displayName)
            ? throw new ArgumentException("Data field display name cannot be empty.", nameof(displayName))
            : displayName;
    }

    public string DisplayName { get; }
}
