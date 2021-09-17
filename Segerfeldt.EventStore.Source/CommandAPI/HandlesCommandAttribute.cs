using JetBrains.Annotations;

using System;

namespace Segerfeldt.EventStore.Source.CommandAPI
{
    [AttributeUsage(AttributeTargets.Class)]
    [MeansImplicitUse]
    public class HandlesCommandAttribute : Attribute
    {
        public string Entity { get; }
        public string? Property { get; init; }
        public bool IsHttpGet { get; init; }

        internal string Pattern => Property is not null
            ? $"/{Entity.ToLowerInvariant()}/{{id}}/{Property}"
            : $"/{Entity.ToLowerInvariant()}";

        public HandlesCommandAttribute(string entity)
        {
            Entity = entity;
        }
    }
}
