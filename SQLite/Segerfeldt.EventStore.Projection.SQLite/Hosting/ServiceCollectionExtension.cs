using Microsoft.Extensions.DependencyInjection;

using Segerfeldt.EventStore.Projection.Hosting;

using System;

namespace Segerfeldt.EventStore.Projection.SQLite.Hosting;

public static class ServiceCollectionExtension
{
    /// <summary>Add an <see cref="EventSource"/> to project events from an SQLite database</summary>
    /// <param name="connectionString">the connection-string to access the database</param>
    /// <param name="name">A unique name for the <see cref="EventSource"/></param>
    /// <returns>An <see cref="EventSourceConfiguration"/> for allowing additional configuration</returns>
    public static EventSourceConfiguration AddHostedSQLiteEventSource(this IServiceCollection services, string connectionString, string name) =>
        services.AddHostedEventSource(name, new SQLiteEventSourceProvider(connectionString));

    /// <summary>Add an <see cref="EventSource"/> to project events from an SQLite database</summary>
    /// <param name="connectionString">the connection-string to access the database</param>
    /// <param name="prepareToReceive">An action to call after setup to prepare to receive event information to the target database</param>
    /// <param name="name">A unique name for the <see cref="EventSource"/></param>
    /// <returns>An <see cref="EventSourceConfiguration"/> for allowing additional configuration</returns>
    public static EventSourceConfiguration AddHostedSQLiteEventSource(this IServiceCollection services, string connectionString, Action<IServiceProvider> prepareToReceive, string name) =>
        services.AddHostedEventSource(name, new SQLiteEventSourceProvider(connectionString) { PrepareToReceive = prepareToReceive });
}
