using System;

namespace Segerfeldt.EventStore.Source
{
    public class PublishedEvent
    {
        public string Name { get; }
        public string Details { get; }
        public string Actor { get; }
        public DateTime Timestamp { get; }

        public PublishedEvent(string name, string details, string actor, DateTime timestamp)
        {
            Name = name;
            Details = details;
            Actor = actor;
            Timestamp = timestamp;
        }
    }
}
