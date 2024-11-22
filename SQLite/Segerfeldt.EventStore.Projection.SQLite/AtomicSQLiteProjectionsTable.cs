using Microsoft.Extensions.DependencyInjection;

using System;
using System.Data;

namespace Segerfeldt.EventStore.Projection.SQLite;

public sealed class AtomicSQLiteProjectionsTable([ServiceKey] string source, TargetDatabase database) : IProjectionTracker
{
    private readonly string source = source;
    private readonly TargetDatabase database = database;

    public static void AddSchema(IDbConnection connection)
    {
        connection.Open();
        using var command = connection.CreateCommand(
            "CREATE TABLE IF NOT EXISTS Projections (source TEXT PRIMARY KEY, position BIGINT)");
        try { command.ExecuteNonQuery(); }
        finally { connection.Close(); }
    }

    public long? GetLastFinishedPosition()
    {
        using var connection = database.CreateConnection();
        using var command = connection.CreateCommand("SELECT position FROM Projections WHERE source = @source");
        command.AddParameter("@source", source);
        connection.Open();
        try { return (long?)command.ExecuteScalar(); }
        finally { connection.Close(); }
    }

    public void OnProjectionFinished(long position, Transaction transaction)
    {
        using var command = transaction.CreateCommand(
            """
            INSERT INTO Projections VALUES (@source, @position)
            ON CONFLICT (source) DO UPDATE
            SET position = @position;
            """);
        command.AddParameter("@source", source);
        command.AddParameter("@position", position);
        command.ExecuteNonQuery();
    }

    public void ProjectingPosition(long position, Action runProjection)
    {
        using var connection = database.CreateConnection();
        connection.Open();
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
