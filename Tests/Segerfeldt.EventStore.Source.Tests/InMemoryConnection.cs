using Microsoft.Data.Sqlite;

using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace Segerfeldt.EventStore.Source.Tests;

internal class InMemoryConnection : DbConnection
{
    private readonly SqliteConnection implementor;

    [AllowNull]
    public override string ConnectionString
    {
        get => implementor.ConnectionString;
        set => implementor.ConnectionString = value;
    }

    public override string Database { get => implementor.Database; }
    public override ConnectionState State { get => implementor.State; }
    public override string DataSource { get => implementor.DataSource; }
    public override string ServerVersion { get => implementor.ServerVersion; }

    public InMemoryConnection()
    {
        implementor = new SqliteConnection("Data Source = :memory:");
        implementor.Open();
    }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => implementor.BeginTransaction();

    public override void ChangeDatabase(string databaseName) { implementor.ChangeDatabase(databaseName); }

    protected override DbCommand CreateDbCommand() => implementor.CreateCommand();

    public override void Open() { }
    public override void Close() { }
}
