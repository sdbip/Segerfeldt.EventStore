namespace Segerfeldt.EventStore.Projection.NUnit;

public static class EventSourceExtension
{
    /// <summary>Mock an <see cref="Event"/> emitted from an <see cref="EventSource"/></summary>
    /// <param name="eventSource">The <see cref="EventSource"/> whose receptacles will receive the mock <see cref="Event"/></param>
    /// <param name="entityId">The id of entity that was (not) updated</param>
    /// <param name="entityType">The type of the entity that was (not) updated</param>
    /// <param name="name">The name of the <see cref="Event"/></param>
    /// <param name="details">The details of the <see cref="Event"/></param>
    public static void MockEmittedEvent(this EventSource eventSource, string entityId, string entityType, string name, object details)
    {
        eventSource.Emit([new Event(entityId, entityType, name, JSON.Serialize(details), 0, 0)], maxCount: 2);
    }
}
