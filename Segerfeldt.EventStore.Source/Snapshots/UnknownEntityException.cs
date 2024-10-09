using System;

namespace Segerfeldt.EventStore.Source.Snapshots;

public sealed class UnknownEntityException(EntityId entityId) : Exception($"No entity with the id '{entityId}' exists.") { }
