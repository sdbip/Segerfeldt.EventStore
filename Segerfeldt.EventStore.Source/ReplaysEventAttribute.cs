using System;

namespace Segerfeldt.EventStore.Source
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ReplaysEventAttribute : Attribute
    {
        public string Event { get; }

        public ReplaysEventAttribute(string @event)
        {
            Event = @event;
        }
    }
}
