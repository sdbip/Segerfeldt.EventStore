using Microsoft.Extensions.DependencyInjection;

using Npgsql;

using Segerfeldt.EventStore.Projection.Hosting;

using System;
using System.Data;

namespace Segerfeldt.EventStore.Projection.PostgreSQL.Hosting;

public static class ServiceCollectionExtension
{
    /// <summary>Add an <see cref="EventSource"/> to project events from a PostgreSQL database</summary>
    /// <param name="name">A unique name for the <see cref="EventSource"/></param>
    /// <param name="connectionString">the connection-string to access the source database</param>
    /// <returns>An <see cref="EventSourceConfiguration"/> for allowing additional configuration</returns>
    public static EventSourceConfiguration AddHostedPostgreSQLEventSource(this IServiceCollection services, string name, string connectionString, EventSourceOptions? options = null) =>
        services.AddHostedPostgreSQLEventSource(name, (p, n) => new NpgsqlConnection(connectionString), options);

    /// <summary>Add an <see cref="EventSource"/> to project events from a PostgreSQL database</summary>
    /// <param name="name">A unique name for the <see cref="EventSource"/></param>
    /// <param name="connection">A connection that access the source database</param>
    /// <returns>An <see cref="EventSourceConfiguration"/> for allowing additional configuration</returns>
    public static EventSourceConfiguration AddHostedPostgreSQLEventSource(this IServiceCollection services, string name, NpgsqlConnection connection, EventSourceOptions? options = null) =>
        services.AddHostedPostgreSQLEventSource(name, (p, n) => connection, options);

    private static EventSourceConfiguration AddHostedPostgreSQLEventSource(this IServiceCollection services, string name, Func<IServiceProvider, object, IDbConnection> connectionFunc, EventSourceOptions? options)
    {
        services.AddKeyedSingleton(name, connectionFunc);
        return services.AddHostedEventSource<PostgreSQLEventSourceRepository>(name, options);
    }
}
