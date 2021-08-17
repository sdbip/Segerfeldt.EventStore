using Segerfeldt.EventStore.Source;

using System;

namespace SourceConsoleApp
{
    internal class Player : EntityBase
    {
        public string Name { get; private set; } = null!;
        public int Score { get; private set; }

        public Player(EntityId id, EntityVersion version) : base(id, version) { }

        public static Player RegisterNew(EntityId id, string name)
        {
            var player = new Player(id, EntityVersion.New) { Name = name };
            player.Add(new UnpublishedEvent("PlayerRegistered", new {Name = name}));
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
            var increment = new ScoreIncrement {Points = points};
            Add(new UnpublishedEvent("ScoreIncreased", increment));
        }

        [ReplaysEvent("PlayerRegistered")]
        public void ReplayPlayerRegistered(Registration registration) { Name = registration.Name; }
        [ReplaysEvent("ScoreIncreased")]
        public void ReplayScoreIncreased(ScoreIncrement increment) { Score += increment.Points; }
    }

    internal record Registration(string Name);
}
