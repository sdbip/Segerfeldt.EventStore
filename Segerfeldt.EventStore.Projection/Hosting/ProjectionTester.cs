using System.Collections.Generic;

namespace Segerfeldt.EventStore.Projection.Hosting;

/// <summary>An object to help testing projections</summary>
public class ProjectionTester
{
    private readonly Dictionary<string, EventSource> eventSources = new();

    internal void Add(EventSource eventSource, string eventSourceName)
    {
        eventSources[eventSourceName] = eventSource;
    }

    /// <summary>Mock emitting events from a named <see cref="EventSource"/></summary>
    /// <param name="eventSourceName">The name used when setting up the event source in services</param>
    /// <param name="events">The events to emit</param>
    public void Emit(string eventSourceName, params Event[] events)
    {
        eventSources[eventSourceName].Emit(events);
    }
}
