using System;

namespace Segerfeldt.EventStore.Source.Tests;

public static class EntityExtension
{
  public static void MockPublishedEvent(this IEntity entity, string name, object details)
  {
    entity.ReplayEvents(new[] { new PublishedEvent(name, JSON.Serialize(details), "", DateTimeOffset.UnixEpoch) });
  }
}
