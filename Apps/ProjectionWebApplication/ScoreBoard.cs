using Segerfeldt.EventStore.Projection;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectionWebApplication
{
    public class ScoreBoard : IProjection
    {
        private Dictionary<string, (string, int)> playerScores = new();

        public IEnumerable<string> HandledEvents => new[] { "PlayerRegistered", "ScoreIncreased" };
        public IEnumerable<(string name, int score)> PlayerScores => playerScores.Select(pair => pair.Value).ToImmutableArray();

        public Task InvokeAsync(Event @event)
        {
            if (@event.Name == "PlayerRegistered")
            {
                var details = @event.DetailsAs<PlayerRegistration>()!;
                playerScores[@event.EntityId] = (details.Name, 0);
            }
            if (@event.Name == "ScoreIncreased")
            {
                var existingValue = playerScores[@event.EntityId];
                var details = @event.DetailsAs<ScoreIncreased>()!;
                playerScores[@event.EntityId] =(existingValue.Item1, existingValue.Item2 + details.Points);
            }
            return Task.CompletedTask;
        }
    }

    public record ScoreIncreased(int Points);
    public record PlayerRegistration(string Name);
}
