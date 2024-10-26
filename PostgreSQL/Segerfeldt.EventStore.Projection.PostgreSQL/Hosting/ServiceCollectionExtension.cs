using Microsoft.Extensions.DependencyInjection;

using Npgsql;

using Segerfeldt.EventStore.Projection.Hosting;

namespace Segerfeldt.EventStore.Projection.PostgreSQL.Hosting;

public static class ServiceCollectionExtension
{
    /// <summary>Add an <see cref="EventSource"/> to project events from an PostgreSQL database</summary>
    /// <param name="connectionString">the connection-string to access the database</param>
    /// <param name="name">A unique name for the <see cref="EventSource"/></param>
    /// <returns>An <see cref="EventSourceConfiguration"/> for allowing additional configuration</returns>
    public static EventSourceConfiguration AddHostedPostgreSQLEventSource(this IServiceCollection services, string connectionString, string name) =>
        services.AddHostedEventSource(name, new NpgsqlConnection(connectionString));
}
