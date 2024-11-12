using JetBrains.Annotations;

using System.Text.Json;

namespace Segerfeldt.EventStore.Source.CommandAPI.DTOs;

/// <summary>Data about an event</summary>
[PublicAPI]
public sealed record Event(string Name, JsonElement Details, string Actor, double Timestamp)
{
    internal static Event From(PublishedEvent @event) =>
        new(@event.Name, @event.DetailsAs<JsonElement>()!,
            @event.Actor, @event.Timestamp.Value);
}
