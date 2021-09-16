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

        internal string Pattern => Property is not null
            ? $"{Entity}/{{id}}/{Property}"
            : $"{Entity}";

        public HandlesCommandAttribute(string entity)
        {
            Entity = entity;
        }
    }
}
