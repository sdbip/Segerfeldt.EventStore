using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Segerfeldt.EventStore.Source
{
    public class StreamPositions
    {
        public long StorePosition { get; }

        private readonly IReadOnlyCollection<(EntityId id, EntityVersion version)> entityVersions;
        public IReadOnlyCollection<EntityId> UpdatedEntityIds => entityVersions.Select(t => t.id).ToList();

        public StreamPositions(long storePosition, IEnumerable<(EntityId Id, EntityVersion version)> entityVersions)
        {
            StorePosition = storePosition;
            this.entityVersions = entityVersions.ToImmutableList();
        }

        public EntityVersion GetVersion(EntityId entityId) =>
            entityVersions.FirstOrDefault(t => t.id == entityId).version;
    }
}
