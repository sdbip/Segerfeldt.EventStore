using Microsoft.Extensions.DependencyInjection;

using System;

namespace Segerfeldt.EventStore.Projection.Hosting;

/// <summary>An object to help testing projections</summary>
public class ProjectionTester(IServiceProvider serviceProvider)
{
    /// <summary>Mock emitting events from a named <see cref="EventSource"/></summary>
    /// <param name="eventSourceName">The name used when setting up the event source in services</param>
    /// <param name="events">The events to emit</param>
    public void Emit(string eventSourceName, params Event[] events)
    {
        var eventSource = serviceProvider.GetRequiredKeyedService<EventSource>(eventSourceName);
        eventSource.Emit(events, maxCount: events.Length + 1);
    }

    public static void EmitInitialEvents(EventSource eventSource)
    {
        eventSource.GetPositionFromTracker();
        eventSource.PollEventsTableOnce();
    }

    public static void EmitNewEvents(EventSource eventSource)
    {
        eventSource.PollEventsTableOnce();
    }
}
