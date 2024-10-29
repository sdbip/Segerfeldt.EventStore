using Npgsql;

using Segerfeldt.EventStore.Source.CommandAPI;

using System;
using System.Data.Common;

namespace Segerfeldt.EventStore.Source.PostgreSQL.CommandAPI;

/// <summary>EventStore provider for a PostgreSQL database that automatically adds the EventStore schema if missing</summary>
/// <param name="connectionString">the connection-string to access the database</param>
public sealed class PostgreSQLEventStoreProvider(string connectionString) : IEventStoreProvider
{
    public void PrepareDatabase(IServiceProvider _) { Schema.CreateIfMissing(CreateConnection()); }
    public DbConnection CreateConnection() => new NpgsqlConnection(connectionString);
}
