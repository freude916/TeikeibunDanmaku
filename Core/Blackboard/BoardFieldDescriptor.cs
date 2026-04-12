using System.Reflection;

namespace TeikeibunDanmaku.Blackboard;

public sealed class BoardFieldDescriptor
{
    public required string Name { get; init; }
    public required Type ValueType { get; init; }
    public required PropertyInfo PropertyInfo { get; init; }
    public required Func<IBoardState, object?> Getter { get; init; }
}
