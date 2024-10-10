using JetBrains.Annotations;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

using System;
using System.Data;

namespace Segerfeldt.EventStore.Projection.Hosting;

[PublicAPI]
public static class ServiceCollectionExtension
{
    /// <summary>Add an <see cref="EventSource"/> to project events</summary>
    /// <param name="services">the services configuration</param>
    /// <param name="connection">a connection object with access to the source database</param>
    /// <param name="eventSourceName">An optional (unique) name for the <see cref="EventSource"/> if you need to access it later</param>
    /// <returns>An <see cref="EventSourceBuilder"/> for allowing additional configuration</returns>
    public static EventSourceBuilder AddHostedEventSource(this IServiceCollection services, IDbConnection connection, string? eventSourceName = null) =>
        services.AddHostedEventSource(_ => connection, eventSourceName);

    /// <summary>Add an <see cref="EventSource"/> to project events</summary>
    /// <param name="services">the services configuration</param>
    /// <param name="createConnection">a function that returns an <see cref="IDbConnection"/> with access the source database</param>
    /// <param name="eventSourceName">An optional (unique) name for the <see cref="EventSource"/> if you need to access it later</param>
    /// <returns>An <see cref="EventSourceBuilder"/> for allowing additional configuration</returns>
    public static EventSourceBuilder AddHostedEventSource(this IServiceCollection services, Func<IServiceProvider, IDbConnection> createConnection, string? eventSourceName = null)
    {
        // The ProjectionTester is only intended as an aid for testing.
        if (eventSourceName != null) services.TryAddSingleton(_ => new ProjectionTester());

        var builder = new EventSourceBuilder(p => new DefaultEventSourceRepository(createConnection(p)));

        // A new hosted service is created for each EventSource.

        // Note: AddHostedService<T>() will only add one service per unique type T. Even if called
        // multiple times. If the user needs to track more than one Source, we'd need a new
        // HostedEventSource *class* for each one. Fortunately, AddSingleton<IHostedService>() does
        // not have such restrictions. And all IHostedServices added *will* be started by the .Net
        // Web API system.

        services.AddSingleton<IHostedService>(p =>
        {
            var eventSource = builder.Build(p);

            // Add the eventSource to ProjectionTester to allow tests to inject events
            // This is not used outside of testing
            if (eventSourceName != null)
            {
                var tester = p.GetRequiredService<ProjectionTester>();
                tester.Add(eventSource, eventSourceName);
            }

            return new HostedEventSource(eventSource);
        });
        return builder;
    }
}
