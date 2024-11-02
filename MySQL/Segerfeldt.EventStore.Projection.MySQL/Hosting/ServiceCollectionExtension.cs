using Microsoft.Extensions.DependencyInjection;

using MySql.Data.MySqlClient;

using Segerfeldt.EventStore.Projection.Hosting;

using System.Data;

namespace Segerfeldt.EventStore.Projection.MySQL.Hosting;

public static class ServiceCollectionExtension
{
    /// <summary>Add an <see cref="EventSource"/> to project events from a MySQL database</summary>
    /// <param name="name">A unique name for the <see cref="EventSource"/></param>
    /// <param name="connectionString">the connection-string to access the source database</param>
    /// <returns>An <see cref="EventSourceConfiguration"/> for allowing additional configuration</returns>
    public static EventSourceConfigurationWithoutTarget AddHostedMySQLEventSource(this IServiceCollection services, string name, string connectionString, EventSourceOptions? options = null) =>
        services.AddHostedMySQLEventSource(name, (p, n) => new MySqlConnection(connectionString), options);

    /// <summary>Add an <see cref="EventSource"/> to project events from a MySQL database</summary>
    /// <param name="name">A unique name for the <see cref="EventSource"/></param>
    /// <param name="connection">A connection that access the source database</param>
    /// <returns>An <see cref="EventSourceConfiguration"/> for allowing additional configuration</returns>
    public static EventSourceConfigurationWithoutTarget AddHostedMySQLEventSource(this IServiceCollection services, string name, MySqlConnection connection, EventSourceOptions? options = null) =>
        services.AddHostedMySQLEventSource(name, (p, n) => connection, options);

    private static EventSourceConfigurationWithoutTarget AddHostedMySQLEventSource(this IServiceCollection services, string name, Func<IServiceProvider, object, IDbConnection> connectionFunc, EventSourceOptions? options)
    {
        services.AddKeyedSingleton(name, connectionFunc);
        return services.AddHostedEventSource<MySQLEventSourceRepository>(name, options);
    }

    public static EventSourceConfiguration SetPostgtreSQLTarget(this EventSourceConfigurationWithoutTarget configuration, string connectionString) =>
        configuration.SetTarget(() => new MySqlConnection(connectionString));
}
