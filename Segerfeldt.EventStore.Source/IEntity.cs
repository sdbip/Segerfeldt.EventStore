using System.Collections.Generic;

namespace Segerfeldt.EventStore.Source
{
    public interface IEntity
    {
        EntityId Id { get; }
        EntityVersion Version { get; }
        IEnumerable<UnpublishedEvent> UnpublishedEvents { get; }

        void ReplayEvents(IEnumerable<PublishedEvent> events);
    }
}
