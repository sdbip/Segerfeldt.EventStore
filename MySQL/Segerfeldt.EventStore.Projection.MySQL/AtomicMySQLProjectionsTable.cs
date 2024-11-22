using Microsoft.Extensions.DependencyInjection;

using System.Data;

namespace Segerfeldt.EventStore.Projection.MySQL;

public sealed class AtomicMySQLProjectionsTable([ServiceKey] string name, IServiceProvider serviceProvider) : IProjectionTracker
{
    private readonly string source = name;
    private readonly TargetDatabase database = serviceProvider.GetRequiredKeyedService<TargetDatabase>(name);

    public static void AddSchema(IDbConnection connection)
    {
        connection.Open();
        using var command = connection.CreateCommand(
            "CREATE TABLE IF NOT EXISTS Projections (source NVARCHAR(256) PRIMARY KEY, position BIGINT)");
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
        connection.CreateCommand("START TRANSACTION").ExecuteNonQuery();
        try { runProjection(); }
        catch
        {
            connection.CreateCommand("ROLLBACK").ExecuteNonQuery();
            return Task.CompletedTask;
        }

        using var command = connection.CreateCommand(
            """
            INSERT INTO Projections VALUES (@source, @position)
            ON DUPLICATE KEY UPDATE
            position = VALUES (position);
            COMMIT
            """);
        command.AddParameter("@source", source);
        command.AddParameter("@position", position);
        try { command.ExecuteNonQuery(); }
        finally { database.CloseSharedConnection(); }
        return Task.CompletedTask;
    }
}
