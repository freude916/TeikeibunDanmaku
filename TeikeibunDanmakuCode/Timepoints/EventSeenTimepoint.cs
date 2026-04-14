using TeikeibunDanmaku.Core.Blackboard;

namespace TeikeibunDanmaku.Timepoints;

public sealed class EventSeenTimepoint : Timepoint<EventState>
{
    public const string TimepointId = "event.seen";
    public const string TimepointDisplayName = "事件出现";

    public override string Id => TimepointId;
    public override string DisplayName => TimepointDisplayName;

    public static EventSeenTimepoint From(string eventId, string eventName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);

        return new EventSeenTimepoint
        {
            State = new EventState
            {
                EventId = eventId,
                EventName = eventName
            }
        };
    }
}
