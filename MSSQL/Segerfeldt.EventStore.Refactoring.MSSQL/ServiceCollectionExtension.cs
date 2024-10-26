using Microsoft.Extensions.DependencyInjection;

using Segerfeldt.EventStore.Refactoring.Hosting;

using System.Data.SqlClient;

namespace Segerfeldt.EventStore.Refactoring.MSSQL;

public static class ServiceCollectionExtension
{
    /// <summary>Add an <see cref="EventSource"/> to project events from an PostgreSQL database</summary>
    /// <param name="connectionString">the connection-string to access the database</param>
    /// <returns>An <see cref="EventSourceConfiguration"/> for allowing additional configuration</returns>
    public static EventSourceConfiguration UseSQLServerRefactoring(this IServiceCollection services, string connectionString) =>
        services.UseRefactoring(_ => new SqlConnection(connectionString));

    /// <summary>Add an <see cref="EventSource"/> to project events from an PostgreSQL database</summary>
    /// <param name="connectionString">the connection-string to access the database</param>
    /// <returns>An <see cref="EventSourceConfiguration"/> for allowing additional configuration</returns>
    public static EventSourceConfiguration UseSQLServerTarget(this EventSourceConfiguration configuration, string connectionString) =>
        configuration.UseTarget(_ =>
        {
            var connection = new SqlConnection(connectionString);
            Schema.CreateIfMissing(connection);
            return connection;
        });
}
