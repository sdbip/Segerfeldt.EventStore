using System.Collections.Generic;
using System.Collections.Immutable;

namespace Segerfeldt.EventStore.Source;

/// <summary>An entity type for namespacing events and verifying the type</summary>
public sealed class EntityType : ValueObject<EntityType>
{
    private readonly string name;

    /// <summary>Initialize a type</summary>
    /// <param name="name">The string value that uniquely identifies the type (and its events)</param>
    public EntityType(string name)
    {
        this.name = name;
    }

    protected override IEnumerable<object> GetEqualityComponents() => ImmutableArray.Create(name);

    public override string ToString() => name;
}
