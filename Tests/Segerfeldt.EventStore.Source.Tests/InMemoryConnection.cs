using System.Data;
using System.Data.SQLite;

namespace Segerfeldt.EventStore.Source.Tests
{
    internal class InMemoryConnection : IDbConnection
    {
        private readonly SQLiteConnection implementor;

        public string ConnectionString
        {
            get => implementor.ConnectionString;
            #nullable disable // This is a fucked up situation!
            set => implementor.ConnectionString = value;
            #nullable enable // The interface is defined as
                             // string ConnectionString { get; [param: AllowNull] set;
                             // What is the point of that!?
        }

        public int ConnectionTimeout => implementor.ConnectionTimeout;
        public string Database => implementor.Database;
        public ConnectionState State => implementor.State;

        public InMemoryConnection() { implementor = new SQLiteConnection("Data Source = :memory:").OpenAndReturn(); }

        public IDbTransaction BeginTransaction() => implementor.BeginTransaction();

        public void Dispose() { implementor.Dispose(); }

        public IDbTransaction BeginTransaction(IsolationLevel il) => throw new System.NotImplementedException();

        public void ChangeDatabase(string databaseName) { implementor.ChangeDatabase(databaseName); }

        public IDbCommand CreateCommand() => implementor.CreateCommand();

        public void Open() { }
        public void Close() { }
    }
}
