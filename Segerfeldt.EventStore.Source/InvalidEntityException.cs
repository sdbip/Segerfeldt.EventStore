using System;

namespace Segerfeldt.EventStore.Source;

internal sealed class InvalidEntityException(Type entityType)
    : Exception($"Invalid entity type {entityType.Name}. Constructor missing.") { }
