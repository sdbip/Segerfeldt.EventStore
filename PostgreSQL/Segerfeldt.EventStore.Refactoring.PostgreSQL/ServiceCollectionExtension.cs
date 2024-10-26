using Microsoft.Extensions.DependencyInjection;

using Npgsql;

using Segerfeldt.EventStore.Refactoring.Hosting;

namespace Segerfeldt.EventStore.Refactoring.PostgreSQL;

public static class ServiceCollectionExtension
{
    /// <summary>Add an <see cref="EventSource"/> to project events from an PostgreSQL database</summary>
    /// <param name="connectionString">the connection-string to access the database</param>
    /// <returns>An <see cref="EventSourceConfiguration"/> for allowing additional configuration</returns>
    public static EventSourceConfiguration UsePostgreSQLRefactoring(this IServiceCollection services, string connectionString) =>
        services.UseRefactoring(_ => new NpgsqlConnection(connectionString));

    /// <summary>Add an <see cref="EventSource"/> to project events from an PostgreSQL database</summary>
    /// <param name="connectionString">the connection-string to access the database</param>
    /// <returns>An <see cref="EventSourceConfiguration"/> for allowing additional configuration</returns>
    public static EventSourceConfiguration UsePostgreSQLTarget(this EventSourceConfiguration configuration, string connectionString) =>
        configuration.UseTarget(_ =>
        {
            var connection = new NpgsqlConnection(connectionString);
            Schema.CreateIfMissing(connection);
            return connection;
        });
}
