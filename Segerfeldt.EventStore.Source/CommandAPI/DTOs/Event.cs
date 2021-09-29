using JetBrains.Annotations;

using System;
using System.Text.Json;

namespace Segerfeldt.EventStore.Source.CommandAPI.DTOs
{
    /// <summary>Data about an event</summary>
    [PublicAPI]
    public sealed record Event
    {
        /// <summary>Name to describe what changed</summary>
        public string Name { get; }
        /// <summary>Details about the change</summary>
        public object? Details { get; }
        /// <summary>The user/actor who made this change</summary>
        public string Actor { get; }
        /// <summary>The time when the change was performed (in milliseconds since the Unix Epoch)</summary>
        public long TimestampMillis { get; }

        private Event(string name, string details, string actor, DateTimeOffset timestamp)
        {
            Actor = actor;
            Name = name;
            Details = JsonSerializer.Deserialize<dynamic>(details);
            TimestampMillis = (timestamp - DateTimeOffset.UnixEpoch).Ticks / 10_000;
        }

        internal static Event From(PublishedEvent @event) =>
            new(@event.Name, @event.Details,
                @event.Actor, @event.Timestamp);
    }
}
