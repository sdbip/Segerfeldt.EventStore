using System;

namespace Segerfeldt.EventStore.Source
{
    internal sealed class InvalidEntityException : Exception
    {
        public InvalidEntityException(Type entityType) : base($"Invalid entity type {entityType.Name}. Constructor missing.") { }
    }
}
