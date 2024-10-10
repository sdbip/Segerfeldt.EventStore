using Segerfeldt.EventStore.Projection.Hosting;

using System;
using System.Data.Common;
using System.Data.SqlClient;

namespace Segerfeldt.EventStore.Projection.MSSQL.Hosting;

/// <summary>EventStore provider for a MS SQL Server database</summary>
/// <param name="connectionString">the connection-string to access the database</param>
public class MSSQLEventSourceProvider(string connectionString) : IEventSourceProvider
{
    public void PrepareDatabase(IServiceProvider _) { }
    public DbConnection CreateConnection() => new SqlConnection(connectionString);
}
