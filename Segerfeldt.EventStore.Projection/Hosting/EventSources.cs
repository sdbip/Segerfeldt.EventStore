using System;
using System.Collections.Generic;

namespace Segerfeldt.EventStore.Projection.Hosting;

public class EventSources
{
    private readonly Dictionary<string, EventSourceBuilder> builders = new();
    private readonly Dictionary<string, EventSource> eventSources = new();
    private IServiceProvider? provider;

    public void Add(EventSourceBuilder builder, string name)
    {
        builders[name] = builder;
    }

    public void Receive(IEnumerable<Event> events, string eventSourceName)
    {
        if (provider is null) throw new Exception("The provider has not been set up");
        if (!eventSources.TryGetValue(eventSourceName, out var eventSource))
        {
            var builder = builders[eventSourceName];
            eventSource = builder.Build(provider);
            eventSources[eventSourceName] = eventSource;
        }

        eventSource.Notify(events);
    }

    // ReSharper disable once ParameterHidesMember
    public void SetProvider(IServiceProvider provider)
    {
        this.provider = provider;
    }
}
