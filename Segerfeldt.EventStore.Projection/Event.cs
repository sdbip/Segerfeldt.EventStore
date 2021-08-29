using System;

namespace Segerfeldt.EventStore.Projection
{
    /// <summary>An event notifying that the state of an entity has changed at the source</summary>
    public class Event
    {
        /// <summary>The id of the entity that changed</summary>
        public string EntityId { get; }
        /// <summary>The name of the event, indicating in how the state has changed</summary>
        public EventName Name { get; }
        /// <summary>Details regarding the change</summary>
        public string Details { get; }
        /// <summary>The position of this event in the stream. Useful for keeping track after restarting the application</summary>
        public long Position { get; }

        public Event(string entityId, EventName name, string details, long position)
        {
            EntityId = entityId;
            Name = name;
            Details = details;
            Position = position;
        }

        public T? DetailsAs<T>() => JSON.Deserialize<T>(Details);
        internal object? DetailsAs(Type type) => JSON.Deserialize(Details, type);
    }
}
