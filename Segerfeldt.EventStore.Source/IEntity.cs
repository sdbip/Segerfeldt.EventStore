using System.Collections.Generic;

namespace Segerfeldt.EventStore.Source;

/// <summary>An (aggregate root) entity in the system.</summary>
/// Or rather the state of the entity at a specific point in time, with changes meant to be applied to that state.
public interface IEntity : IIdentifiable
{
    /// <summary>The type of this entity, used for type-checking by the <see cref="EntityStore"/></summary>
    EntityType Type { get; }
    /// <summary>The version (optimistic concurrency lock) of this entity when last reconstituted.</summary>
    /// If this value is different in the database, there will have been concurrent changes outside this
    /// process. Those changes invalidate any changes done here. The current operation will have to be aborted
    /// unless the entity can be reconstituted to the updated state and the operation replayed from there.
    EntityVersion Version { get; }
    /// <summary>Events that should be published when publishing changes in the <see cref="EntityStore"/></summary>
    IEnumerable<UnpublishedEvent> UnpublishedEvents { get; }

    /// <summary>Replay published events to reconstitute the state of the entity</summary>
    /// <param name="events">All the published events for this entity</param>
    /// Calling this method should update the state of the entity so that consequent operations can
    /// be allowed or denied correctly, and so that allowed operations generate the correct events.
    void ReplayEvents(IEnumerable<PublishedEvent> events);
}
