using System.Text.Json;

namespace Segerfeldt.EventStore.Projection
{
    /// <summary>An event notifying that the state of an entity has changed at the source</summary>
    public class Event
    {
        private static readonly JsonSerializerOptions CamelCase = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase};

        /// <summary>The id of the entity that changed</summary>
        public string EntityId { get; }
        /// <summary>The name of the event, signifying what about the entity changed</summary>
        public string Name { get; }
        /// <summary>Details regarding the change</summary>
        public string Details { get; }
        /// <summary>The position of this event in the stream. Useful when updating a database and restarting the application</summary>
        public long Position { get; }

        public Event(string entityId, string name, string details, long position)
        {
            Name = name;
            Details = details;
            Position = position;
            EntityId = entityId;
        }

        public T? DetailsAs<T>() => JsonSerializer.Deserialize<T>(Details, CamelCase);
    }
}
