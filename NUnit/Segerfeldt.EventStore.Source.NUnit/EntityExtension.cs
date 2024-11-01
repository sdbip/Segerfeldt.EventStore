using Segerfeldt.EventStore.Shared;
using Segerfeldt.EventStore.Source.Internals;

namespace Segerfeldt.EventStore.Source.NUnit;

public static class EntityExtension
{
    /// <summary>Replay a mock <see cref="PublishedEvent"/> as if it came from the <see cref="EntityStore"/></summary>
    /// <param name="entity">The tested entity</param>
    /// <param name="name">The name of the event</param>
    /// <param name="details">The details of the event (to be serialized as JSON)</param>
    public static void MockPublishedEvent(this IEntity entity, string name, object details)
    {
        entity.ReplayEvents([new PublishedEvent(name, JSON.Serialize(details), "", Timestamp.UnixEpoch)]);
    }
}
