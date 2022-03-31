using System;

namespace Segerfeldt.EventStore.Projection;

/// <summary>An event notifying that the state of an entity has changed at the source</summary>
public sealed class Event
{
    /// <summary>The id of the entity that changed</summary>
    public string EntityId { get; }
    /// <summary>The type of entity publishing this event, used as a namespace for duplicated event names</summary>
    public string EntityType { get; }
    /// <summary>The name of the event, indicating in what way the entity's state has changed</summary>
    public string Name { get; }
    /// <summary>A JSON object specifying the details of the change</summary>
    public string Details { get; }
    /// <summary>The position of this event in the stream. Useful for keeping track after restarting the application</summary>
    public long Position { get; }

    public Event(string entityId, string name, string entityType, string details, long position)
    {
        EntityId = entityId;
        Name = name;
        EntityType = entityType;
        Details = details;
        Position = position;
    }

    public T? DetailsAs<T>() => JSON.Deserialize<T>(Details);
    internal object? DetailsAs(Type type) => JSON.Deserialize(Details, type);
}
