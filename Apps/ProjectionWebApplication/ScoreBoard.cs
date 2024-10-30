using Segerfeldt.EventStore.Projection;

using System;
using System.Collections.Generic;

namespace ProjectionWebApplication;

public sealed class ScoreBoard(TargetDatabase database) : ReceptacleBase
{
    private readonly TargetDatabase database = database;

    public IEnumerable<(string name, int score)> PlayerScores
    {
        get
        {
            var connection = this.database.CreateConnection();
            var command = connection.CreateCommand("""
            SELECT * FROM Players
            """);

            connection.Open();
            try { return command.ExecuteReader().AllRowsAs(r => ((string)r["name"], Convert.ToInt32(r["score"]))); }
            finally { connection.Close(); }
        }
    }

    [ReceivesEvent("PlayerRegistered")]
    public void ReceivePlayerRegistered(string entityId, PlayerRegistration details)
    {
        var transaction = database.Transaction ?? throw new Exception("No transaction??");
        using var command = transaction.CreateCommand("""
            INSERT INTO Players VALUES (@id, @name, 0)
            """);
        command.AddParameter("@id", entityId);
        command.AddParameter("@name", details.Name);
        command.ExecuteNonQuery();
    }

    [ReceivesEvent("ScoreIncreased")]
    public void ReceiveScoreIncreased(string entityId, ScoreIncrement details)
    {
        var transaction = database.Transaction ?? throw new Exception("No transaction??");
        using var command = transaction.CreateCommand("""
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
