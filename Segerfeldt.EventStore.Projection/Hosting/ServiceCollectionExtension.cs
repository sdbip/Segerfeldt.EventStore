using JetBrains.Annotations;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

using System;

namespace Segerfeldt.EventStore.Projection.Hosting;

[PublicAPI]
public static class ServiceCollectionExtension
{
    /// <summary>Add an <see cref="EventSource"/> to project events</summary>
    /// <param name="services">the services configuration</param>
    /// <param name="connectionPool">an <see cref="IConnectionPool"/> that accesses the source database</param>
    /// <param name="eventSourceName">An optional (unique) name for the <see cref="EventSource"/> if you need to access it later</param>
    /// <<returns>An <see cref="EventSourceBuilder"/> for allowing additional configuration</returns>
    public static EventSourceBuilder AddHostedEventSource(this IServiceCollection services, IConnectionPool connectionPool, string? eventSourceName = null) =>
        services.AddHostedEventSource(_ => connectionPool, eventSourceName);

    /// <summary>Add an <see cref="EventSource"/> to project events</summary>
    /// <typeparam name="TConnectionPool">The type of a Singleton <see cref="IConnectionPool"/> that </typeparam>
    /// <param name="services">the services configuration</param>
    /// <param name="eventSourceName">An optional (unique) name for the <see cref="EventSource"/> if you need to access it later</param>
    /// <<returns>An <see cref="EventSourceBuilder"/> for allowing additional configuration</returns>
    public static EventSourceBuilder AddHostedEventSource<TConnectionPool>(this IServiceCollection services, string? eventSourceName = null) where TConnectionPool : IConnectionPool =>
        services.AddHostedEventSource(p => p.GetRequiredService<TConnectionPool>(), eventSourceName);

    /// <summary>Add an <see cref="EventSource"/> to project events</summary>
    /// <param name="services">the services configuration</param>
    /// <param name="getConnectionPool">a function that returns an <see cref="IConnectionPool"/> that accesses the source database</param>
    /// <param name="eventSourceName">An optional (unique) name for the <see cref="EventSource"/> if you need to access it later</param>
    /// <<returns>An <see cref="EventSourceBuilder"/> for allowing additional configuration</returns>
    public static EventSourceBuilder AddHostedEventSource(this IServiceCollection services, Func<IServiceProvider, IConnectionPool> getConnectionPool, string? eventSourceName = null)
    {
        if (eventSourceName != null) services.TryAddSingleton(p => new EventSources());

        var builder = new EventSourceBuilder(p => new DefaultEventSourceRepository(getConnectionPool(p)));

        // A new hosted service is created for each EventSource.

        // Note: AddHostedService<T>() will only add one service per unique type T. Even if called
        // multiple times. If the user needs to track more than one Source, we'd need a new
        // HostedEventSource *class* for each one. Fortunately, AddSingleton<IHostedService>() does
        // not have such restrictions. And all IHostedServices added *will* be started by the .Net
        // Web API system.

        services.AddSingleton<IHostedService>(p =>
        {
            var eventSource = builder.Build(p);

            // Add the eventSource to EventSources to allow tests to inject events
            // This is not used outside of testing
            if (eventSourceName != null)
            {
                var sources = p.GetRequiredService<EventSources>();
                sources.Add(eventSource, eventSourceName);
            }

            return new HostedEventSource(eventSource);
        });
        return builder;
    }
}
