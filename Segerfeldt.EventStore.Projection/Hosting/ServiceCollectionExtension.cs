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

        var builder = new EventSourceBuilder(p => new DefaultEventSourceRepository(getConnectionPool(p)));

        services.TryAddSingleton(new EventSources());
        var sources = (EventSources?) services.First(s => s.ServiceType == typeof(EventSources)).ImplementationInstance;
        sources?.Add(builder, eventSourceName);

        services.AddSingleton<IHostedService>(p =>
        {
            var sources = p.GetRequiredService<EventSources>();
            sources.Add(builder.Build(p), eventSourceName);

            return new HostedEventSource(sources[eventSourceName]);
        });
        return builder;
    }
}
