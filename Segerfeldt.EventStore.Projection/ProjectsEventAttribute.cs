using JetBrains.Annotations;

using System;

namespace Segerfeldt.EventStore.Projection
{
    [AttributeUsage(AttributeTargets.Method)]
    [MeansImplicitUse]
    public class ProjectsEventAttribute : Attribute
    {
        public string Event { get; }

        public ProjectsEventAttribute(string @event)
        {
            Event = @event;
        }
    }
}
