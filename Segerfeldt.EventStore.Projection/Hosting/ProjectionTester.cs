using System.Collections.Generic;

namespace Segerfeldt.EventStore.Projection.Hosting;

public class ProjectionTester
{
    private readonly Dictionary<string, EventSource> eventSources = new();

    internal void Add(EventSource eventSource, string eventSourceName)
    {
        eventSources[eventSourceName] = eventSource;
    }

    public void Emit(string eventSourceName, params Event[] events)
    {
        eventSources[eventSourceName].Emit(events);
    }
}
