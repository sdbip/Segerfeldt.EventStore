using Segerfeldt.EventStore.Source.Internals;

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
            connection.Open();
            connection.CreateCommand(schemaSQL).ExecuteNonQuery();
            connection.Close();
        }

        private static string ReadSchemaSQLResource()
        {
            var sqliteAssembly = typeof(SQLServer).Assembly;
            var sqliteNamespace = typeof(SQLServer).Namespace;
            return new StreamReader(sqliteAssembly.GetManifestResourceStream($"{sqliteNamespace}.schema.sql")!).ReadToEnd();
        }
    }
}
