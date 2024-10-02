using System.Collections.Generic;

namespace Segerfeldt.EventStore.Projection.Hosting;

public class EventSources
{
    private readonly Dictionary<string, EventSource> eventSources = new();

    public void Add(EventSource eventSource, string eventSourceName)
    {
        eventSources[eventSourceName] = eventSource;
    }

    public void Receive(IEnumerable<Event> events, string eventSourceName)
    {
        eventSources[eventSourceName].Notify(events);
    }
}
