using System;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Segerfeldt.EventStore.Source;

public sealed class IncorrectTypeException(EntityType expectedType, EntityType actualType)
    : Exception($"Entity has the wrong typ. Actual type is {actualType}, expected {expectedType}")
{
    public EntityType ExpectedType { get; } = expectedType;
    public EntityType ActualType { get; } = actualType;
}
