using Microsoft.Extensions.DependencyInjection;

using System;
using System.Data;
using System.Threading.Tasks;

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

    public Task ProjectingPosition(long position, Action runProjection)
    {
        using var connection = database.OpenSharedConnection();
        connection.CreateCommand("BEGIN TRANSACTION").ExecuteNonQuery();
        try { runProjection(); }
        catch
        {
            connection.CreateCommand("ROLLBACK TRANSACTION").ExecuteNonQuery();
            return Task.CompletedTask;
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
        finally { database.CloseSharedConnection(); }
        return Task.CompletedTask;
    }
}
