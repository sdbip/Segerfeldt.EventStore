using System.Collections.Generic;

namespace Segerfeldt.EventStore.Source
{
    public class EntityHistory
    {
        public EntityVersion Version { get; }
        public IEnumerable<PublishedEvent> Events { get; }

        public EntityHistory(EntityVersion version, IEnumerable<PublishedEvent> events)
        {
            Version = version;
            Events = events;
        }
    }
}
