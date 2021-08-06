using System;

namespace Segerfeldt.EventStore.Source
{
    public sealed class ConcurrentUpdateException : Exception
    {
        public EntityVersion ExpectedVersion { get; }
        public EntityVersion ActualVersion { get; }

        public ConcurrentUpdateException(EntityVersion expectedVersion, EntityVersion actualVersion) : base($"Entity has been modified. Current version is {actualVersion}, expected {expectedVersion}")
        {
            ExpectedVersion = expectedVersion;
            ActualVersion = actualVersion;
        }
    }
}
