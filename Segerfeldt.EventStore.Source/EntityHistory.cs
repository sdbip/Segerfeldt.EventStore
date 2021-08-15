using System.Collections.Generic;

namespace Segerfeldt.EventStore.Source
{
    /// <summary>The history information of an entity</summary>
    public class EntityHistory
    {
        /// <summary>The current version (last event) of the entity</summary>
        public EntityVersion Version { get; }
        /// <summary>All the events that have been logged to define the state of the entity</summary>
        public IEnumerable<PublishedEvent> Events { get; }

        public EntityHistory(EntityVersion version, IEnumerable<PublishedEvent> events)
        {
            Version = version;
            Events = events;
        }
    }
}
