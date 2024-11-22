using Microsoft.Data.Sqlite;

using System;

namespace Segerfeldt.EventStore.Projection.SQLite;

public sealed class AtomicSQLiteProjectionsTable(string source, SqliteConnection connection)
{
    private readonly SqliteConnection connection = connection;
    private readonly string source = source;

    public static void AddSchema(SqliteConnection connection)
    {
        connection.Open();
        using var command = connection.CreateCommand(
            "CREATE TABLE IF NOT EXISTS Projections (source TEXT PRIMARY KEY, position BIGINT)");
        try { command.ExecuteNonQuery(); }
        finally { connection.Close(); }
    }

    public long? GetLastFinishedPosition()
    {
        using var command = connection.CreateCommand("SELECT position FROM Projections WHERE source = @source");
        command.AddParameter("@source", source);
        try { return (long?)command.ExecuteScalar(); }
        finally { connection.Close(); }
    }

    public void ProjectingPosition(long position, Action runProjection)
    {
        connection.CreateCommand("BEGIN TRANSACTION").ExecuteNonQuery();
        try { runProjection(); }
        catch
        {
            connection.CreateCommand("ROLLBACK TRANSACTION").ExecuteNonQuery();
            return;
        }
        using var command = connection.CreateCommand(
            """
            INSERT INTO Projections VALUES (@source, @position)
            ON CONFLICT (source) DO UPDATE
            SET position = @position;
            COMMIT TRANSACTION
            """);
        command.AddParameter("@source", source);
        command.AddParameter("@position", position);
        try { command.ExecuteNonQuery(); }
        finally { connection.Close(); }
    }
}
