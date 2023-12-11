using System.Collections.Generic;

namespace Segerfeldt.EventStore.Source;

/// <summary>An entity in the system. Entities are the carriers of system state</summary>
public interface IEntity : IIdentifiable
{
    /// <summary>The type of the entity</summary>
    EntityType Type { get; }
    /// <summary>The version of this entity when reconstituted from storage.</summary>
    /// If this value is stored different in the database, there will have been concurrent changes outside
    /// this process, which invalidate any changes done here. Either the current operation will have to be
    /// aborted, or the entity must be reconstituted to the updated state, and the operation repeated.
    EntityVersion Version { get; }
    /// <summary>Events that should be published when publishing changes in the <see cref="EntityStore"/></summary>
    IEnumerable<UnpublishedEvent> UnpublishedEvents { get; }

    /// <summary>Replay published events to reconstitute the state of the entity</summary>
    /// <param name="events">All the published events for this entity</param>
    /// Calling this method should update the state of the entity so that consequent operations can
    /// be allowed or denied correctly, and so that allowed operations generate the correct events.
    void ReplayEvents(IEnumerable<PublishedEvent> events);
}
