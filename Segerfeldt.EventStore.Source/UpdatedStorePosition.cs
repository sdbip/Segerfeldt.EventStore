using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Segerfeldt.EventStore.Source
{
    public sealed class UpdatedStorePosition
    {
        public long Position { get; }

        private readonly IReadOnlyDictionary<EntityId, EntityVersion> entityVersions;
        public IReadOnlyCollection<EntityId> UpdatedEntityIds => entityVersions.Keys.ToList();

        public UpdatedStorePosition(long position, IEnumerable<(EntityId id, EntityVersion version)> entityVersions)
        {
            Position = position;
            this.entityVersions = entityVersions.ToImmutableDictionary(x => x.id, x => x.version);
        }

        public EntityVersion GetVersion(EntityId entityId) => entityVersions[entityId];
    }
}
