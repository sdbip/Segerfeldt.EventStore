using System.Data;
using System.Data.SQLite;

namespace Segerfeldt.EventStore.Source
{
    // ReSharper disable once InconsistentNaming
    public sealed class SqLiteConnectionFactory : IConnectionFactory
    {
        private readonly string connectionString;

        public SqLiteConnectionFactory(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public IDbConnection CreateConnection() => new SQLiteConnection(connectionString);

        public void CreateSchemaIfMissing(IDbConnection connection)
        {
            connection.CreateCommand(
                    "CREATE TABLE Entities (id TEXT, version INT, PRIMARY KEY (id));" +
                    "CREATE TABLE Events (entity TEXT, name TEXT, details TEXT, actor TEXT, timestamp INT DEFAULT CURRENT_TIMESTAMP, version INT, position INT, FOREIGN KEY (entity) REFERENCES Entities (id))")
                .ExecuteNonQuery();
        }
    }
}
