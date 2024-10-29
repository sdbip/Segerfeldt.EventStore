using Segerfeldt.EventStore.Source.CommandAPI;

using System;
using System.Data.Common;
using System.Data.SqlClient;

namespace Segerfeldt.EventStore.Source.MSSQL.CommandAPI;

/// <summary>EventStore provider for a MS SQL Server database that automatically adds the EventStore schema if missing</summary>
/// <param name="connectionString">the connection-string to access the database</param>
public sealed class SQLServerEventStoreProvider(string connectionString) : IEventStoreProvider
{
    public void PrepareDatabase(IServiceProvider _) { Schema.CreateIfMissing(CreateConnection()); }
    public DbConnection CreateConnection() => new SqlConnection(connectionString);
}
