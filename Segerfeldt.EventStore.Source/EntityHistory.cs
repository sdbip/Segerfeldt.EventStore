using System.Collections.Generic;

namespace Segerfeldt.EventStore.Source;

/// <summary>The history information of an entity</summary>
public sealed class EntityHistory(EntityType type, EntityVersion version, IEnumerable<PublishedEvent> events)
{
    /// <summary>The event-namespacing type of the entity</summary>
    public EntityType Type { get; } = type;
    /// <summary>The current version (last event) of the entity</summary>
    public EntityVersion Version { get; } = version;
    /// <summary>All the events that have been logged to define the state of the entity</summary>
    public IEnumerable<PublishedEvent> Events { get; } = events;
}
