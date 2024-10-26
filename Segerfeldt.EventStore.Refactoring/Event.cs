using System;

using Segerfeldt.EventStore.Shared;

namespace Segerfeldt.EventStore.Refactoring;

/// <summary>An event notifying that the state of an entity has changed at the source</summary>
public sealed class Event(string entityId, string entityType, string name, string details, int ordinal, long position, string actor, double timestamp)
{
    /// <summary>The id of the entity that changed</summary>
    public string EntityId { get; } = entityId;
    /// <summary>The type of entity publishing this event, used as a namespace for duplicated event names</summary>
    public string EntityType { get; } = entityType;
    /// <summary>The name of the event, indicating in what way the entity's state has changed</summary>
    public string Name { get; } = name;
    /// <summary>A JSON object specifying the details of the change</summary>
    public string Details { get; } = details;
    /// <summary>The ordinal of this event in the entity stream</summary>
    public int Ordinal { get; } = ordinal;
    public long Position { get; } = position;
    public string Actor { get; } = actor;
    public double Timestamp { get; } = timestamp;

    public T? DetailsAs<T>() => JSON.Deserialize<T>(Details);
    internal object? DetailsAs(Type type) => JSON.Deserialize(Details, type);
}
