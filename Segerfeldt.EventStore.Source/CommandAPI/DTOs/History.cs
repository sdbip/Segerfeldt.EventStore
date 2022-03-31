using JetBrains.Annotations;

using System.Collections.Generic;
using System.Linq;

namespace Segerfeldt.EventStore.Source.CommandAPI.DTOs;

/// <summary>The history of an entity</summary>
[PublicAPI]
public sealed record History
{
    /// <summary>The type of the entity represented by this history</summary>
    public string Type { get; }
    /// <summary>The version of the entity</summary>
    public int Version { get; }
    /// <summary>The events that have occurred to this entity so far</summary>
    public IEnumerable<Event> Events { get; } = null!;

    private History(string type, int version, IEnumerable<Event> events)
    {
        Type = type;
        Version = version;
        Events = events;
    }

    internal static History From(EntityHistory history) =>
        new(history.Type.ToString(),
            history.Version.Value,
            history.Events.Select(Event.From));
}
