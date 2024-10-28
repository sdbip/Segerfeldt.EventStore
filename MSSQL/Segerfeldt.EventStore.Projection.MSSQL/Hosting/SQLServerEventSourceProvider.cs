using Segerfeldt.EventStore.Projection.Hosting;

using System;
using System.Data.SqlClient;

namespace Segerfeldt.EventStore.Projection.MSSQL.Hosting;

public class SQLServerEventSourceProvider(string connectionString) : IEventSourceProvider
{
    private readonly string connectionString = connectionString;

    public Action<IServiceProvider> PrepareToReceive { get; set; } = _ => { };

    public IEventSourceRepository CreateRepository() => new SQLServerEventSourceRepository(new SqlConnection(connectionString));
}
