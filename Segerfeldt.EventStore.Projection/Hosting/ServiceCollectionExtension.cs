using JetBrains.Annotations;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

using System;
using System.Linq;

namespace Segerfeldt.EventStore.Projection.Hosting;

[PublicAPI]
public static class ServiceCollectionExtension
{
    public delegate void NamedBuilderDelegate(EventSourceBuilder builder, string name);

    public static EventSourceBuilder AddHostedEventSource(this IServiceCollection services, IConnectionPool connectionPool, string eventSourceName) =>
        services.AddHostedEventSource(_ => connectionPool, eventSourceName);

    public static EventSourceBuilder AddHostedEventSource<TConnectionPool>(this IServiceCollection services, string eventSourceName) where TConnectionPool : IConnectionPool =>
        services.AddHostedEventSource(p => p.GetRequiredService<TConnectionPool>(), eventSourceName);

    public static EventSourceBuilder AddHostedEventSource(this IServiceCollection services, Func<IServiceProvider, IConnectionPool> getConnectionPool, string eventSourceName)
    {
        // Note: AddHostedService<T>() will only add one service per unique type T. Even if called
        // multiple times. If the user needs to track more than one Source, we'd need a new
        // HostedEventSource *class* for each one. Fortunately, AddSingleton<IHostedService>() does
        // not have such restrictions. And all IHostedServices added *will* be started by the .Net
        // Web API system.

        services.TryAddSingleton(new EventSources());

        var builder = new EventSourceBuilder(p => new DefaultEventSourceRepository(getConnectionPool(p)));

        // A new hosted service is created for each event-source.
        services.AddSingleton<IHostedService>(p =>
        {
            var eventSource = builder.Build(p);

            // Add the eventSource to EventSources to allow tests to inject events
            // This is not used outside of testing
            var sources = p.GetRequiredService<EventSources>();
            sources.Add(eventSource, eventSourceName);

            return new HostedEventSource(eventSource);
        });
        return builder;
    }
}
