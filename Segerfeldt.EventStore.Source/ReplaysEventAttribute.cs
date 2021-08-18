using JetBrains.Annotations;

using System;

namespace Segerfeldt.EventStore.Source
{
    [AttributeUsage(AttributeTargets.Method)]
    [MeansImplicitUse]
    public class ReplaysEventAttribute : Attribute
    {
        public string Event { get; }

        public ReplaysEventAttribute(string @event)
        {
            Event = @event;
        }
    }
}
