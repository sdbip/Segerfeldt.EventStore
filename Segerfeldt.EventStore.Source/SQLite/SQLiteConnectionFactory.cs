using System.Data;
using System.Data.SQLite;
using System.IO;

namespace Segerfeldt.EventStore.Source.SQLite
{
    // ReSharper disable once InconsistentNaming
    public sealed class SQLiteConnectionFactory : IConnectionFactory
    {
        private readonly string connectionString;

        public SQLiteConnectionFactory(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public IDbConnection CreateConnection() => new SQLiteConnection(connectionString);

        public void CreateSchemaIfMissing(IDbConnection connection)
        {
            var schemaSQL = ReadSchemaSQLResource();
            connection.CreateCommand(schemaSQL).ExecuteNonQuery();
        }

        private static string ReadSchemaSQLResource()
        {
            var sqliteAssembly = typeof(SQLiteConnectionFactory).Assembly;
            var sqliteNamespace = typeof(SQLiteConnectionFactory).Namespace;
            return new StreamReader(sqliteAssembly.GetManifestResourceStream($"{sqliteNamespace}.schema.sql")!).ReadToEnd();
        }
    }
}
