using Microsoft.Data.Sqlite;

using Segerfeldt.EventStore.Projection.Hosting;

using System;
using System.Data.Common;

namespace Segerfeldt.EventStore.Projection.SQLite.Hosting;

/// <summary>EventStore provider for an SQLite database</summary>
/// <param name="connectionString">the connection-string to access the database</param>
public class SQLiteEventSourceProvider(string connectionString) : IEventSourceProvider
{
    public void PrepareDatabase(IServiceProvider _) { }
    public DbConnection CreateConnection() => new SqliteConnection(connectionString);
}
