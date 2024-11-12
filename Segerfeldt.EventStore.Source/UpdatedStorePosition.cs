using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Segerfeldt.EventStore.Source;

public sealed class UpdatedStorePosition(Position position, IEnumerable<(EntityId id, EntityVersion version)> entityVersions)
{
    /// <summary>the new position after the updaate</summary>
    public Position Position { get; } = position;

    private readonly IReadOnlyDictionary<EntityId, EntityVersion> entityVersions = entityVersions.ToImmutableDictionary(x => x.id, x => x.version);
    /// <summary>The ids of the updated enities</summary>
    public IReadOnlyCollection<EntityId> UpdatedEntityIds => entityVersions.Keys.ToList();

    /// <summary>The new version of an updated entity</summary>
    /// <param name="entityId"></param>
    /// <returns></returns>
    public EntityVersion? GetVersion(EntityId entityId) =>
        entityVersions.TryGetValue(entityId, out var version) ? version : null;
}
