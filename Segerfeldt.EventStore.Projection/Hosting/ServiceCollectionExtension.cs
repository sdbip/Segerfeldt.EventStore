using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using System;

namespace Segerfeldt.EventStore.Projection.Hosting;

public static class ServiceCollectionExtension
{
    public static EventSourceBuilder AddHostedEventSource(this IServiceCollection services, IConnectionPool connectionPool) =>
        services.AddHostedEventSource(_ => connectionPool);

    public static EventSourceBuilder AddHostedEventSource<TConnectionPool>(this IServiceCollection services) where TConnectionPool : IConnectionPool =>
        services.AddHostedEventSource(p => p.GetRequiredService<TConnectionPool>());

    public static EventSourceBuilder AddHostedEventSource(this IServiceCollection services, Func<IServiceProvider, IConnectionPool> getConnectionPool)
    {
        // Note: AddHostedService<T>() will only add one service per unique type T. Even if called
        // multiple times. If the user needs to track more than one Source, we'd need a new
        // HostedEventSource *class* for each one. Fortunately, AddSingleton<IHostedService>() does
        // not have such restrictions. And all IHostedServices added *will* be started by the .Net
        // Web API system.

        var builder = new EventSourceBuilder(getConnectionPool);
        services.AddSingleton<IHostedService>(p => new HostedEventSource(builder.Build(p)));
        return builder;
    }
}
