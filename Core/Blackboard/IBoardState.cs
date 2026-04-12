using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace TeikeibunDanmaku.Blackboard;

public interface IBoardState
{
    IReadOnlyDictionary<string, BoardFieldDescriptor> FieldDescriptors => BoardStateRegistry.GetFieldDescriptors(GetType());
}

public static class BoardStateRegistry
{
    private static readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, BoardFieldDescriptor>> _descriptorCache = new();

    public static IReadOnlyDictionary<string, BoardFieldDescriptor> GetFieldDescriptors(Type type)
    {
        return _descriptorCache.GetOrAdd(type, BuildDescriptors);
    }

    private static IReadOnlyDictionary<string, BoardFieldDescriptor> BuildDescriptors(Type stateType)
    {
        return stateType
            .GetProperties()
            .Where(p => Attribute.IsDefined(p, typeof(DataFieldAttribute)))
            .ToDictionary(p => p.Name, BuildDescriptor);
    }

    private static BoardFieldDescriptor BuildDescriptor(PropertyInfo property)
    {
        var getterMethod = property.GetMethod;
        var declaringType = property.DeclaringType;
        if (getterMethod == null || declaringType == null)
            throw new InvalidOperationException($"Data field '{property.Name}' must have a public getter.");

        var stateParameter = Expression.Parameter(typeof(IBoardState), "state");
        var castedState = Expression.Convert(stateParameter, declaringType);
        var callGetter = Expression.Call(castedState, getterMethod);
        var boxedValue = Expression.Convert(callGetter, typeof(object));
        var getter = Expression.Lambda<Func<IBoardState, object?>>(boxedValue, stateParameter).Compile();

        return new BoardFieldDescriptor
        {
            Name = property.Name,
            ValueType = property.PropertyType,
            PropertyInfo = property,
            Getter = getter
        };
    }
}
