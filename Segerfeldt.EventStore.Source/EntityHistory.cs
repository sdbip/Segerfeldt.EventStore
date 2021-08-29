using System.Collections.Generic;

namespace Segerfeldt.EventStore.Source
{
    /// <summary>The history information of an entity</summary>
    public class EntityHistory
    {
        /// <summary>The event-namespacing type of the entity</summary>
        public EntityType Type { get; }
        /// <summary>The current version (last event) of the entity</summary>
        public EntityVersion Version { get; }
        /// <summary>All the events that have been logged to define the state of the entity</summary>
        public IEnumerable<PublishedEvent> Events { get; }

        public EntityHistory(EntityType type, EntityVersion version, IEnumerable<PublishedEvent> events)
        {
            Type = type;
            Version = version;
            Events = events;
        }
    }
}
