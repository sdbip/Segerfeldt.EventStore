using System.Data;
using System.IO;

namespace Segerfeldt.EventStore.Source.SQLite
{
    // ReSharper disable once InconsistentNaming
    public static class SQLite
    {
        public static void CreateSchemaIfMissing(IDbConnection connection)
        {
            var schemaSQL = ReadSchemaSQLResource();
            connection.CreateCommand(schemaSQL).ExecuteNonQuery();
        }

        private static string ReadSchemaSQLResource()
        {
            var sqliteAssembly = typeof(SQLite).Assembly;
            var sqliteNamespace = typeof(SQLite).Namespace;
            return new StreamReader(sqliteAssembly.GetManifestResourceStream($"{sqliteNamespace}.schema.sql")!).ReadToEnd();
        }
    }
}
