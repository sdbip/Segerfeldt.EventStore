using Segerfeldt.EventStore.Shared;

namespace Segerfeldt.EventStore.Projection.NUnit;

public static class EventSourceExtension
{
  public static void MockNotifiedEvent(this EventSource eventSource, string entityId, string entityType, string name, object details)
  {
    eventSource.Emit(new[] { new Event(entityId, name, entityType, JSON.Serialize(details), 0, 0) });
  }
}
