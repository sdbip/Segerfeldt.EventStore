using Segerfeldt.EventStore.Source;

using System;
using System.Threading.Tasks;

namespace SourceConsoleApp;

internal sealed class Player(EntityId id, EntityVersion version) : EntityBase(id, EntityType, version)
{
    public static readonly EntityType EntityType = new("Player");

    private const string PlayerRegistered = "PlayerRegistered";
    private const string ScoreIncreased = "ScoreIncreased";

    public string Name { get; private set; } = null!;
    public int Score { get; private set; }

    public static async Task<Player?> ReconstituteAsync(EntityId entityId, EntityStore store) =>
        await store.ReconstituteAsync<Player>(entityId, EntityType);

    public static Player RegisterNew(EntityId id, string name)
    {
        var player = new Player(id, EntityVersion.New) { Name = name };
        player.Add(new UnpublishedEvent(PlayerRegistered, new Registration(name)));
        return player;
    }

    public void AwardPoints(int points)
    {
        switch (points)
        {
            case < 0: throw new ArgumentOutOfRangeException(nameof(points), "Must not award a negative number of points.");
            case 0: return; // Unnecessary to add event if it doesn't change the state.
        }

        Score += points;
        Add(new UnpublishedEvent(ScoreIncreased, new ScoreIncrement(points)));
    }

    [ReplaysEvent(PlayerRegistered)]
    public void ReplayPlayerRegistered(Registration registration) { Name = registration.Name; }
    [ReplaysEvent(ScoreIncreased)]
    public void ReplayScoreIncreased(ScoreIncrement increment) { Score += increment.Points; }
}

internal record Registration(string Name);
internal record ScoreIncrement(int Points);
