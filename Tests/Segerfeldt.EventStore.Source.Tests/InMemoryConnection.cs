using System.Data;
using System.Data.Common;
using System.Data.SQLite;

namespace Segerfeldt.EventStore.Source.Tests;

internal class InMemoryConnection : DbConnection
{
    private readonly SQLiteConnection implementor;

    public override string ConnectionString
    {
        get => implementor.ConnectionString;
#nullable disable // This is a fucked up situation!
        set => implementor.ConnectionString = value;
#nullable enable // The interface is defined as
        // string ConnectionString { get; [param: AllowNull] set;
        // What is the point of that!?
    }

    public override string Database => implementor.Database;
    public override ConnectionState State => implementor.State;
    public override string DataSource => implementor.DataSource!;
    public override string ServerVersion => implementor.ServerVersion!;

    public InMemoryConnection() { implementor = new SQLiteConnection("Data Source = :memory:").OpenAndReturn(); }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => implementor.BeginTransaction();

    public override void ChangeDatabase(string databaseName) { implementor.ChangeDatabase(databaseName); }

    protected override DbCommand CreateDbCommand() => implementor.CreateCommand();

    public override void Open() { }
    public override void Close() { }
}
