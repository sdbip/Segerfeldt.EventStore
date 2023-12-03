using System;
using System.Collections.Generic;

namespace Segerfeldt.EventStore.Projection.Hosting;

public class EventSources
{
    private readonly Dictionary<string, EventSourceBuilder> builders = new();
    private readonly Dictionary<string, EventSource> eventSources = new();
    private IServiceProvider? provider;

    public EventSource this[string eventSourceName]
    {
        get
        {
            if (!eventSources.TryGetValue(eventSourceName, out var eventSource))
            {
                if (!builders.TryGetValue(eventSourceName, out var builder))
                    throw new Exception($"There is no source named {eventSourceName}");
                if (provider is null)
                    throw new Exception($"The IServiceProvider has not been set up");

                eventSource = eventSources[eventSourceName] = builder.Build(provider);
            }

            return eventSource;
        }
    }

    internal void Add(EventSourceBuilder builder, string eventSourceName)
    {
        builders[eventSourceName] = builder;
    }

    public void Add(EventSource eventSource, string eventSourceName)
    {
        eventSources[eventSourceName] = eventSource;
    }

    public void NotifyNewEvents(string eventSourceName)
    {
        this[eventSourceName].NotifyNewEvents();
    }

    public void Receive(IEnumerable<Event> events, string eventSourceName)
    {
        this[eventSourceName].Notify(events);
    }

    public void SetProvider(IServiceProvider provider)
    {
        this.provider = provider;
    }
}
