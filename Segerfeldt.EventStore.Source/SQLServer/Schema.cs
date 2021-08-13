using Segerfeldt.EventStore.Source.Internals;

using System.Data;
using System.IO;

namespace Segerfeldt.EventStore.Source.SQLServer
{
    // ReSharper disable once InconsistentNaming
    public static class Schema
    {
        public static void CreateIfMissing(IDbConnection connection)
        {
            var schemaSQL = ReadSchemaSQLResource();
            connection.Open();
            try { connection.CreateCommand(schemaSQL).ExecuteNonQuery(); }
            finally { connection.Close(); }
        }

        private static string ReadSchemaSQLResource()
        {
            var sqliteAssembly = typeof(Schema).Assembly;
            var sqliteNamespace = typeof(Schema).Namespace;
            return new StreamReader(sqliteAssembly.GetManifestResourceStream($"{sqliteNamespace}.schema.sql")!).ReadToEnd();
        }
    }
}
