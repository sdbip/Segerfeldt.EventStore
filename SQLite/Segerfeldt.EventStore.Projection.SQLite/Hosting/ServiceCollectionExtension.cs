using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

using Segerfeldt.EventStore.Projection.Hosting;

using System;
using System.Data;

namespace Segerfeldt.EventStore.Projection.SQLite.Hosting;

public static class ServiceCollectionExtension
{
    /// <summary>Add an <see cref="EventSource"/> to project events from an SQLite database</summary>
    /// <param name="name">A unique name for the <see cref="EventSource"/></param>
    /// <param name="connectionString">the connection-string to access the source database</param>
    /// <returns>An <see cref="EventSourceConfiguration"/> for allowing additional configuration</returns>
    public static EventSourceConfigurationWithoutTarget AddHostedSQLiteEventSource(this IServiceCollection services, string name, string connectionString, EventSourceOptions? options = null) =>
        services.AddHostedSQLiteEventSource(name, (p, n) => new SqliteConnection(connectionString), options);

    /// <summary>Add an <see cref="EventSource"/> to project events from an SQLite database</summary>
    /// <param name="name">A unique name for the <see cref="EventSource"/></param>
    /// <param name="connection">A connection that access the source database</param>
    /// <returns>An <see cref="EventSourceConfiguration"/> for allowing additional configuration</returns>
    public static EventSourceConfigurationWithoutTarget AddHostedSQLiteEventSource(this IServiceCollection services, string name, SqliteConnection connection, EventSourceOptions? options = null) =>
        services.AddHostedSQLiteEventSource(name, (p, n) => connection, options);

    private static EventSourceConfigurationWithoutTarget AddHostedSQLiteEventSource(this IServiceCollection services, string name, Func<IServiceProvider, object, IDbConnection> connectionFunc, EventSourceOptions? options)
    {
        services.AddKeyedSingleton(name, connectionFunc);
        return services.AddHostedEventSource<SQLiteEventSourceRepository>(name, options);
    }

    public static EventSourceConfiguration SetSQLiteTarget(this EventSourceConfigurationWithoutTarget configuration, string connectionString) =>
        configuration.SetTarget(() => new SqliteConnection(connectionString));
}
