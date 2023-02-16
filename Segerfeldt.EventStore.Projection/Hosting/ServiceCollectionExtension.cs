using JetBrains.Annotations;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using System;

namespace Segerfeldt.EventStore.Projection.Hosting;

[PublicAPI]
public static class ServiceCollectionExtension
{
    public delegate void NamedBuilderDelegate(EventSourceBuilder builder, string name);
    public static event NamedBuilderDelegate? BuilderCreated;

    public static EventSourceBuilder AddHostedEventSource(this IServiceCollection services, IConnectionPool connectionPool, string? eventSourceName = null) =>
        services.AddHostedEventSource(_ => connectionPool, eventSourceName);

    public static EventSourceBuilder AddHostedEventSource<TConnectionPool>(this IServiceCollection services, string? name = null) where TConnectionPool : IConnectionPool =>
        services.AddHostedEventSource(p => p.GetRequiredService<TConnectionPool>(), name);

    public static EventSourceBuilder AddHostedEventSource(this IServiceCollection services, Func<IServiceProvider, IConnectionPool> getConnectionPool, string? eventSourceName = null)
    {
        // Note: AddHostedService<T>() will only add one service per unique type T. Even if called
        // multiple times. If the user needs to track more than one Source, we'd need a new
        // HostedEventSource *class* for each one. Fortunately, AddSingleton<IHostedService>() does
        // not have such restrictions. And all IHostedServices added *will* be started by the .Net
        // Web API system.

        var builder = new EventSourceBuilder(getConnectionPool);

        // To enable testing: Allow NUnit to know when a builder is created (WebApplicationFactory)
        if (eventSourceName is not null) BuilderCreated?.Invoke(builder, eventSourceName);

        services.AddSingleton<IHostedService>(p => new HostedEventSource(builder.Build(p)));
        return builder;
    }
}
