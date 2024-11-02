using Microsoft.Extensions.DependencyInjection;

using MySql.Data.MySqlClient;

using Segerfeldt.EventStore.Refactoring.Hosting;

namespace Segerfeldt.EventStore.Refactoring.MySQL;

public static class ServiceCollectionExtension
{
    /// <summary>Add an <see cref="EventSource"/> to project events from an MySQL database</summary>
    /// <param name="connectionString">the connection-string to access the database</param>
    /// <returns>An <see cref="EventSourceConfiguration"/> for allowing additional configuration</returns>
    public static EventSourceConfiguration UseMySQLRefactoring(this IServiceCollection services, string connectionString) =>
        services.UseRefactoring(_ => new MySqlConnection(connectionString));

    /// <summary>Add an <see cref="EventSource"/> to project events from an MySQL database</summary>
    /// <param name="connectionString">the connection-string to access the database</param>
    /// <returns>An <see cref="EventSourceConfiguration"/> for allowing additional configuration</returns>
    public static EventSourceConfiguration UseMySQLTarget(this EventSourceConfiguration configuration, string connectionString) =>
        configuration.UseTarget(_ =>
        {
            var connection = new MySqlConnection(connectionString);
            Schema.CreateIfMissing(connection);
            return connection;
        });
}
