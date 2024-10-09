using System;

using Segerfeldt.EventStore.Shared;

namespace Segerfeldt.EventStore.Source.NUnit;

public static class EntityExtension
{
    public static void MockPublishedEvent(this IEntity entity, string name, object details)
    {
        entity.ReplayEvents(new[] { new PublishedEvent(name, JSON.Serialize(details), "", DateTimeOffset.UnixEpoch) });
    }
}
