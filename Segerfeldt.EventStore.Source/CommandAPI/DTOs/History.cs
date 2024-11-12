using JetBrains.Annotations;

using System.Collections.Generic;
using System.Linq;

namespace Segerfeldt.EventStore.Source.CommandAPI.DTOs;

/// <summary>The history of an entity</summary>
[PublicAPI]
public record History(string Type, int Version, IEnumerable<Event> Events)
{
    internal static History From(EntityHistory history) =>
        new(history.Type.ToString(),
            history.Version.Value,
            history.Events.Select(Event.From));
}
