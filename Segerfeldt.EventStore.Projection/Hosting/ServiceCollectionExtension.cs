using JetBrains.Annotations;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using System;
using System.Data;

namespace Segerfeldt.EventStore.Projection.Hosting;

internal class SingletonEventSource(IDbConnection connection) : IEventSourceProvider
{
    public void PrepareToReceive(IServiceProvider serviceProvider) { }
    public IDbConnection CreateConnection() => connection;
}

[PublicAPI]
public static class ServiceCollectionExtension
{
    /// <summary>Add an <see cref="EventSource"/> to project events</summary>
    /// <param name="services">the Web API builder services</param>
    /// <param name="name">A unique name for the <see cref="EventSource"/></param>
    /// <param name="connection">A connection to the sourcce write-model database</param>
    /// <returns>An <see cref="EventSourceConfiguration"/> for allowing additional configuration</returns>
    public static EventSourceConfiguration AddHostedEventSource(this IServiceCollection services, string name, IDbConnection connection) =>
        AddHostedEventSource(services, name, new SingletonEventSource(connection));

    /// <summary>Add an <see cref="EventSource"/> to project events</summary>
    /// <param name="services">the Web API builder services</param>
    /// <param name="name">A unique name for the <see cref="EventSource"/></param>
    /// <param name="provider">an object that knows how to create connections to the write-model database</param>
    /// <returns>An <see cref="EventSourceConfiguration"/> for allowing additional configuration</returns>
    public static EventSourceConfiguration AddHostedEventSource(this IServiceCollection services, string name, IEventSourceProvider provider)
    {
        services.AddKeyedSingleton(name, new EventSourceRepository(provider.CreateConnection()));
        services.AddKeyedSingleton(name, (p, n) => new EventSource(
            p.GetRequiredKeyedService<EventSourceRepository>(n),
            p.GetRequiredKeyedService<ReceptacleCollection>(n),
            p.GetKeyedService<IProjectionTracker>(n),
            p.GetKeyedService<IPollingStrategy>(n)));

        var configuration = new EventSourceConfiguration(services, name);

        // A new hosted service is created for each EventSource.

        // Note: AddHostedService<T>() will only add one service per unique type T. Even if called
        // multiple times. If the user needs to track more than one Source, we'd need a new
        // HostedEventSource *class* for each one. Fortunately, AddSingleton<IHostedService>() does
        // not have such restrictions. And all IHostedServices added *will* be started by the .Net
        // Web API system.

        services.AddSingleton<IHostedService>(p =>
        {
            provider.PrepareToReceive(p);
            return new HostedEventSource(p.GetRequiredKeyedService<EventSource>(configuration.name));
        });
        return configuration;
    }
}
