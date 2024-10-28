using Microsoft.Data.Sqlite;

using Segerfeldt.EventStore.Projection.Hosting;

using System;

namespace Segerfeldt.EventStore.Projection.SQLite.Hosting;

public sealed class SQLiteEventSourceProvider(string connectionString) : IEventSourceProvider
{
    public Action<IServiceProvider> PrepareToReceive { get; set; } = _ => { };

    public IEventSourceRepository CreateRepository() => new SQLiteEventSourceRepository(new SqliteConnection(connectionString));
}
