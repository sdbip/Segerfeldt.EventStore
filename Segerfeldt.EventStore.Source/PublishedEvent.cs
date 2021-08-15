using System;

namespace Segerfeldt.EventStore.Source
{
    /// <summary>An event that has been published and is part of the official state of an entity</summary>
    public class PublishedEvent
    {
        /// <summary>A name identifying what aspect of the entity changed</summary>
        public string Name { get; }
        /// <summary>A JSON-serialized object that specifies the details of the change</summary>
        public string Details { get; }
        /// <summary>The user that published this event</summary>
        public string Actor { get; }
        /// <summary>The instant this event was published</summary>
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
