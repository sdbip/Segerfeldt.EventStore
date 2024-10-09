using Segerfeldt.EventStore.Projection;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ProjectionWebApplication;

public sealed class ScoreBoard : ReceptacleBase
{
    private readonly Dictionary<string, (string name, int score)> playerScores = new();

    public IEnumerable<(string name, int score)> PlayerScores => playerScores.Select(pair => pair.Value).ToImmutableArray();

    [ReceivesEvent("PlayerRegistered")]
    public void ReceivePlayerRegistered(string entityId, PlayerRegistration details)
    {
        playerScores[entityId] = (details.Name, 0);
    }

    [ReceivesEvent("ScoreIncreased")]
    public void ReceiveScoreIncreased(string entityId, ScoreIncrement details)
    {
        var (unchangingName, previousScore) = playerScores[entityId];
        playerScores[entityId] = (unchangingName, previousScore + details.Points);
    }
}

// ReSharper disable ClassNeverInstantiated.Global
public record ScoreIncrement(int Points);
public record PlayerRegistration(string Name);
