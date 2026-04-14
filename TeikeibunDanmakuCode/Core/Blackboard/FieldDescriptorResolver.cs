using System.Collections.Concurrent;

namespace TeikeibunDanmaku.Core.Blackboard;

public static class FieldDescriptorResolver
{
    private static readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, BoardFieldDescriptor>> Cache = new();

    public static IReadOnlyDictionary<string, BoardFieldDescriptor> GetFieldDescriptors(Type stateType)
    {
        ArgumentNullException.ThrowIfNull(stateType);
        return Cache.GetOrAdd(stateType, BuildDescriptors);
    }

    private static IReadOnlyDictionary<string, BoardFieldDescriptor> BuildDescriptors(Type stateType)
    {
        var baseDescriptors = BoardStateRegistry.GetFieldDescriptors(stateType);
        var merged = new Dictionary<string, BoardFieldDescriptor>(baseDescriptors, StringComparer.Ordinal);

        foreach (var pair in LongStateFieldRegistry.GetDescriptors())
        {
            merged.TryAdd(pair.Key, pair.Value);
        }

        return merged;
    }
}
