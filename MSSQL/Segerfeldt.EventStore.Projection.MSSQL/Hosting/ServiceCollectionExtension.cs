using Microsoft.Extensions.DependencyInjection;

using Segerfeldt.EventStore.Projection.Hosting;

using System.Data.SqlClient;

namespace Segerfeldt.EventStore.Projection.MSSQL.Hosting;

public static class ServiceCollectionExtension
{
    /// <summary>Add an <see cref="EventSource"/> to project events from an MS SQL Server database</summary>
    /// <param name="connectionString">the connection-string to access the database</param>
    /// <param name="name">A unique name for the <see cref="EventSource"/></param>
    /// <returns>An <see cref="EventSourceConfiguration"/> for allowing additional configuration</returns>
    public static EventSourceConfiguration AddHostedSQLServerEventSource(this IServiceCollection services, string connectionString, string name) =>
        services.AddHostedEventSource(name, new SqlConnection(connectionString));
}
