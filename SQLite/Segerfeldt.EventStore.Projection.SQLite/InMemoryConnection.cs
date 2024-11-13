using Microsoft.Data.Sqlite;

namespace Segerfeldt.EventStore.Projection.SQLite;

public sealed class InMemoryConnection : SqliteConnection
{
    public InMemoryConnection() : base("Data Source = :memory:") => base.Open();

    public override void Open() { }
    public override void Close() { }
}
