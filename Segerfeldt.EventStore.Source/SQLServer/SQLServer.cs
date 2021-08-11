using System.Data;
using System.IO;

namespace Segerfeldt.EventStore.Source.SQLServer
{
    // ReSharper disable once InconsistentNaming
    public static class SQLServer
    {
        public static void CreateSchemaIfMissing(IDbConnection connection)
        {
            var schemaSQL = ReadSchemaSQLResource();
            connection.CreateCommand(schemaSQL).ExecuteNonQuery();
        }

        private static string ReadSchemaSQLResource()
        {
            var sqliteAssembly = typeof(SQLServer).Assembly;
            var sqliteNamespace = typeof(SQLServer).Namespace;
            return new StreamReader(sqliteAssembly.GetManifestResourceStream($"{sqliteNamespace}.schema.sql")!).ReadToEnd();
        }
    }
}
