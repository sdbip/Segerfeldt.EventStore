using System;

namespace Segerfeldt.EventStore.Source.Snapshots
{
    public sealed class UnknownEntityException : Exception
    {
        public UnknownEntityException(EntityId entityId) : base($"No entity with the id '{entityId}' exists.")
        {
        }
    }
}
