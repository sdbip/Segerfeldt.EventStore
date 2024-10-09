using System;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Segerfeldt.EventStore.Source;

public sealed class ConcurrentUpdateException(EntityVersion expectedVersion, EntityVersion actualVersion)
    : Exception($"Entity has been modified. Current version is {actualVersion}, expected {expectedVersion}")
{
    public EntityVersion ExpectedVersion { get; } = expectedVersion;
    public EntityVersion ActualVersion { get; } = actualVersion;
}
