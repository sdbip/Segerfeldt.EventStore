using Segerfeldt.EventStore.Projection;
using Segerfeldt.EventStore.Shared;

using System.Collections.Generic;

namespace ProjectionWebApplication;

public sealed class ScoreBoard(TargetDbConnection connection) : ReceptacleBase
{
    private readonly TargetDbConnection connection = connection;

    public IEnumerable<(string name, int score)> PlayerScores
    {
        get
        {
            var connection = this.connection.WithoutTransaction;
            var command = connection.CreateCommand("""
            SELECT * FROM Players
            """);

            connection.Open();
            try { return command.ExecuteReader().AllRowsAs(r => ((string)r["name"], (int)r["score"])); }
            finally { connection.Close(); }
        }
    }

    [ReceivesEvent("PlayerRegistered")]
    public void ReceivePlayerRegistered(string entityId, PlayerRegistration details)
    {
        using var command = connection.CreateCommand("""
            INSERT INTO Players VALUES (@id, @name, 0)
            """);
        command.AddParameter("@id", entityId);
        command.AddParameter("@name", details.Name);
        command.ExecuteNonQuery();
    }

    [ReceivesEvent("ScoreIncreased")]
    public void ReceiveScoreIncreased(string entityId, ScoreIncrement details)
    {
        using var command = connection.CreateCommand("""
            UPDATE Players SET score = score + @points
                WHERE id = @id
            """);
        command.AddParameter("@id", entityId);
        command.AddParameter("@points", details.Points);
        command.ExecuteNonQuery();
    }
}

// ReSharper disable ClassNeverInstantiated.Global
public record ScoreIncrement(int Points);
public record PlayerRegistration(string Name);
