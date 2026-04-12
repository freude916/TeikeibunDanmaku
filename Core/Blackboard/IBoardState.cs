using System.Collections.Concurrent;
using System.Reflection;

namespace TeikeibunDanmaku.Blackboard;

public interface IBoardState
{
    Dictionary<string, PropertyInfo> Fields => BoardStateRegistry.GetFields(this.GetType());
}

// 1. 定义一个专门存储字段信息的泛型容器
public static class BoardStateRegistry
{
    private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> _cache = new();

    public static Dictionary<string, PropertyInfo> GetFields(Type type)
    {
        return _cache.GetOrAdd(type, t => 
        {
            return t.GetProperties()
                .Where(p => Attribute.IsDefined(p, typeof(DataFieldAttribute)))
                .ToDictionary(p => p.Name, p => p);
        });
    }
}