using Npgsql;

using Segerfeldt.EventStore.Projection.Hosting;

using System;

namespace Segerfeldt.EventStore.Projection.PostgreSQL.Hosting;

internal class PostgreSQLEventSourceProvider(string? connectionString) : IEventSourceProvider
{
    private readonly string? connectionString = connectionString;

    public Action<IServiceProvider> PrepareToReceive { get; set; } = _ => { };

    public IEventSourceRepository CreateRepository() => new PostgreSQLEventSourceRepository(new NpgsqlConnection(connectionString));
}
