using System;

using Segerfeldt.EventStore.Shared;

namespace Segerfeldt.EventStore.Source;

/// <summary>An event that has been published and is part of the official state of an entity</summary>
public sealed class PublishedEvent(string name, string details, string actor, DateTimeOffset timestamp)
{
    /// <summary>A name identifying what aspect of the entity changed</summary>
    public string Name { get; } = name;
    /// <summary>A JSON-serialized object that specifies the details of the change</summary>
    public string Details { get; } = details;
    /// <summary>The user that published this event</summary>
    public string Actor { get; } = actor;
    /// <summary>The instant this event was published</summary>
    public DateTimeOffset Timestamp { get; } = timestamp;

    /// <summary>Parses the JSON details as a specific type</summary>
    /// <typeparam name="T">The desired type</typeparam>
    /// <returns>The typed object if it can be deserialized, null if it cannot</returns>
    public T? DetailsAs<T>() => JSON.Deserialize<T>(Details);
    internal object? DetailsAs(Type type) => JSON.Deserialize(Details, type);
}
