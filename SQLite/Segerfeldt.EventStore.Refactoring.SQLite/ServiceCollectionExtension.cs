using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

using Segerfeldt.EventStore.Refactoring.Hosting;

namespace Segerfeldt.EventStore.Refactoring.SQLite;

public static class ServiceCollectionExtension
{
    /// <summary>Add an <see cref="EventSource"/> to project events from an SQLite database</summary>
    /// <param name="connectionString">the connection-string to access the database</param>
    /// <returns>An <see cref="EventSourceConfiguration"/> for allowing additional configuration</returns>
    public static EventSourceConfiguration UseSQLiteRefactoring(this IServiceCollection services, string connectionString) =>
        services.UseRefactoring(_ => new SqliteConnection(connectionString));

    /// <summary>Add an <see cref="EventSource"/> to project events from an SQLite database</summary>
    /// <param name="connectionString">the connection-string to access the database</param>
    /// <returns>An <see cref="EventSourceConfiguration"/> for allowing additional configuration</returns>
    public static EventSourceConfiguration UseSQLiteTarget(this EventSourceConfiguration configuration, string connectionString) =>
        configuration.UseTarget(_ =>
        {
            var connection = new SqliteConnection(connectionString);
            Schema.CreateIfMissing(connection);
            return connection;
        });
}
