using Segerfeldt.EventStore.Projection;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ProjectionWebApplication
{
    public class ScoreBoard : ProjectorBase
    {
        private readonly Dictionary<string, (string name, int score)> playerScores = new();

        public IEnumerable<(string name, int score)> PlayerScores => playerScores.Select(pair => pair.Value).ToImmutableArray();

        [ProjectsEvent("PlayerRegistered")]
        public void ProjectPlayerRegistered(string entityId, PlayerRegistration details)
        {
            playerScores[entityId] = (details.Name, 0);
        }

        [ProjectsEvent("ScoreIncreased")]
        public void ProjectScoreIncreased(string entityId, ScoreIncreased details)
        {
            var existingValue = playerScores[entityId];
            playerScores[entityId] = (existingValue.name, existingValue.score + details.Points);
        }
    }

    public record ScoreIncreased(int Points);
    public record PlayerRegistration(string Name);
}
