using System;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Segerfeldt.EventStore.Source;

public sealed class IncorrectTypeException : Exception
{
    public EntityType ExpectedType { get; }
    public EntityType ActualType { get; }

    public IncorrectTypeException(EntityType expectedType, EntityType actualType) : base($"Entity has the wrong typ. Actual type is {actualType}, expected {expectedType}")
    {
        ExpectedType = expectedType;
        ActualType = actualType;
    }
}
