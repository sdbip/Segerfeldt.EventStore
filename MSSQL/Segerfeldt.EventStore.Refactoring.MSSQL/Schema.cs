using System.Data;
using System.IO;

namespace Segerfeldt.EventStore.Refactoring.MSSQL;

// ReSharper disable once InconsistentNaming
public static class Schema
{
    /// <summary>Adds the Entities and Events tables to a database if they don't already exist.</summary>
    /// <param name="connection">a connection to the database to add the tables to</param>
    /// Uses SQLite syntax to create tables
    public static void CreateIfMissing(IDbConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = ReadSchemaSQLResource();
        connection.Open();
        try { command.ExecuteNonQuery(); }
        finally { connection.Close(); }
    }

    private static string ReadSchemaSQLResource()
    {
        var sqliteAssembly = typeof(Schema).Assembly;
        var sqliteNamespace = typeof(Schema).Namespace;
        return new StreamReader(sqliteAssembly.GetManifestResourceStream($"{sqliteNamespace}.schema.sql")!).ReadToEnd();
    }
}
