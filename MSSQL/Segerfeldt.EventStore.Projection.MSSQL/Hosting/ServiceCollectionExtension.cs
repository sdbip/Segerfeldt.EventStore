using Microsoft.Extensions.DependencyInjection;

using Segerfeldt.EventStore.Projection.Hosting;

using System;
using System.Data;
using System.Data.SqlClient;

namespace Segerfeldt.EventStore.Projection.MSSQL.Hosting;

public static class ServiceCollectionExtension
{
    /// <summary>Add an <see cref="EventSource"/> to project events from a SQL Server database</summary>
    /// <param name="name">A unique name for the <see cref="EventSource"/></param>
    /// <param name="connectionString">the connection-string to access the source database</param>
    /// <returns>An <see cref="EventSourceConfiguration"/> for allowing additional configuration</returns>
    public static EventSourceConfigurationWithoutTarget AddHostedSQLServerEventSource(this IServiceCollection services, string name, string connectionString, EventSourceOptions? options = null) =>
        services.AddHostedSQLServerEventSource(name, (p, n) => new SqlConnection(connectionString), options);

    /// <summary>Add an <see cref="EventSource"/> to project events from a SQL Server database</summary>
    /// <param name="name">A unique name for the <see cref="EventSource"/></param>
    /// <param name="connection">A connection that access the source database</param>
    /// <returns>An <see cref="EventSourceConfiguration"/> for allowing additional configuration</returns>
    public static EventSourceConfigurationWithoutTarget AddHostedSQLServerEventSource(this IServiceCollection services, string name, SqlConnection connection, EventSourceOptions? options = null) =>
        services.AddHostedSQLServerEventSource(name, (p, n) => connection, options);

    private static EventSourceConfigurationWithoutTarget AddHostedSQLServerEventSource(this IServiceCollection services, string name, Func<IServiceProvider, object, IDbConnection> connectionFunc, EventSourceOptions? options)
    {
        services.AddKeyedSingleton(name, connectionFunc);
        return services.AddHostedEventSource<SQLServerEventSourceRepository>(name, options);
    }

    public static EventSourceConfiguration SetSQLServerTarget(this EventSourceConfigurationWithoutTarget configuration, string connectionString) =>
        configuration.SetTarget(() => new SqlConnection(connectionString));
}
