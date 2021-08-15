using System.Text.Json;

namespace Segerfeldt.EventStore.Projection
{
    public class Event
    {
        private static readonly JsonSerializerOptions CamelCase = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase};
        public string EntityId { get; }
        public string Name { get; }
        public string Details { get; }
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
