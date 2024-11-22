using Microsoft.Extensions.DependencyInjection;

using System;
using System.Data;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Projection.MSSQL;

public sealed class AtomicSQLServerProjectionsTable([ServiceKey] string name, IServiceProvider serviceProvider) : IProjectionTracker
{
    private readonly string source = name;
    private readonly TargetDatabase database = serviceProvider.GetRequiredKeyedService<TargetDatabase>(name);

    public static void AddSchema(IDbConnection connection)
    {
        connection.Open();
        using var command = connection.CreateCommand(
            "IF OBJECT_ID('Projections') IS NULL CREATE TABLE Projections (source NVARCHAR(256) PRIMARY KEY, position BIGINT)");
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
        var transaction = connection.BeginTransaction();
        try { runProjection(); }
        catch
        {
            transaction.Rollback();
            return Task.CompletedTask;
        }

        using var command = transaction.CreateCommand(
            """
            MERGE INTO Projections
            USING (VALUES (@source, @position)) AS new (source, position)
            ON Projections.source = new.source
            WHEN MATCHED THEN
                UPDATE SET position = new.position
            WHEN NOT MATCHED THEN
                INSERT (source, position) VALUES (new.source, new.position);
            """);
        command.AddParameter("@source", source);
        command.AddParameter("@position", position);
        try
        {
            command.ExecuteNonQuery();
            transaction.Commit();
        }
        finally { database.CloseSharedConnection(); }
        return Task.CompletedTask;
    }
}
