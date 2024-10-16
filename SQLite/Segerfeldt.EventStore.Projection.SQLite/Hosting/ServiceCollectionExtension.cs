using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

using Segerfeldt.EventStore.Projection.Hosting;

namespace Segerfeldt.EventStore.Projection.SQLite.Hosting;

public static class ServiceCollectionExtension
{
    /// <summary>Add an <see cref="EventSource"/> to project events from an SQLite database</summary>
    /// <param name="connectionString">the connection-string to access the database</param>
    /// <param name="eventSourceName">An optional (unique) name for the <see cref="EventSource"/> if you need to access it later</param>
    /// <returns>An <see cref="EventSourceBuilder"/> for allowing additional configuration</returns>
    public static EventSourceBuilder AddHostedSQLiteEventSource(this IServiceCollection services, string connectionString, string? eventSourceName = null) =>
        services.AddHostedEventSource(() => new SqliteConnection(connectionString), eventSourceName);
}
