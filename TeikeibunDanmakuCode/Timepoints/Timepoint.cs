using System.Collections.Concurrent;
using System.Reflection;
using TeikeibunDanmaku.Core.Blackboard;

namespace TeikeibunDanmaku.Timepoints;

public abstract class Timepoint
{
    public abstract string Id { get; }
    public abstract string DisplayName { get; }
    public abstract IBoardState BoardState { get; }
    
    public abstract Type StateType { get; }
    
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;

    public void Publish()
    {
        TimepointBus.Publish(this);
    }
}

public readonly record struct TimepointDescriptor(string Id, string DisplayName, Type StateType);

public abstract class Timepoint<TState>: Timepoint where TState : IBoardState
{
    public override IBoardState BoardState => State;
    
    public required TState State { get; init; }

    public override Type StateType => TimepointRegistry.GetStateType(GetType());
}

public static class TimepointRegistry
{
    private static readonly ConcurrentDictionary<Type, Type> _stateCache = new();
    
    public static Type GetStateType(Type type)
    {
        return _stateCache.GetOrAdd(type, BuildStateType);
    }

    private static Type BuildStateType(Type timepointType)
    {
        return timepointType.GetProperty("State")!.PropertyType;
    }
}
