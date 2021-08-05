using System;

namespace Segerfeldt.EventStore.Source
{
    public sealed class ConcurrentUpdateException : Exception
    {
        public EntityVersion ExpectedVersion { get; }
        public int? ActualVersion { get; }

        public ConcurrentUpdateException(EntityVersion expectedVersion, int? actualVersion) : base($"Entity has been modified. Current version is {actualVersion}, expected {expectedVersion}")
        {
            ExpectedVersion = expectedVersion;
            ActualVersion = actualVersion;
        }
    }
}
