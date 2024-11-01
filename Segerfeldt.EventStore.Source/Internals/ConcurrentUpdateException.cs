using System;

namespace Segerfeldt.EventStore.Source.Internals;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
public sealed class ConcurrentUpdateException(EntityVersion expectedVersion, EntityVersion actualVersion)
    : Exception($"Entity has been modified. Current version is {actualVersion}, expected {expectedVersion}")
{
    public EntityVersion ExpectedVersion { get; } = expectedVersion;
    public EntityVersion ActualVersion { get; } = actualVersion;
}
