using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Segerfeldt.EventStore.Source;

public sealed class UpdatedStorePosition(long position, IEnumerable<(EntityId id, EntityVersion version)> entityVersions)
{
    public long Position { get; } = position;

    private readonly IReadOnlyDictionary<EntityId, EntityVersion> entityVersions = entityVersions.ToImmutableDictionary(x => x.id, x => x.version);
    public IReadOnlyCollection<EntityId> UpdatedEntityIds => entityVersions.Keys.ToList();

    public EntityVersion GetVersion(EntityId entityId) => entityVersions[entityId];
}
