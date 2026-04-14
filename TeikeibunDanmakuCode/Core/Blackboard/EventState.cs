namespace TeikeibunDanmaku.Core.Blackboard;

public sealed class EventState : IBoardState
{
    [DataField("事件ID")] public required string EventId { get; init; }

    [DataField("事件名")] public required string EventName { get; init; }
}
