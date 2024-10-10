using Npgsql;

using Segerfeldt.EventStore.Projection.Hosting;

using System;
using System.Data.Common;

namespace Segerfeldt.EventStore.Projection.PostgreSQL.Hosting;

/// <summary>EventStore provider for a PostgreSQL database that automatically adds the EventStore schema if missing</summary>
/// <param name="connectionString">the connection-string to access the database</param>
public class PostgreSQLEventSourceProvider(string connectionString) : IEventSourceProvider
{
    public void PrepareDatabase(IServiceProvider _) { }
    public DbConnection CreateConnection() => new NpgsqlConnection(connectionString);
}
