using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace Segerfeldt.EventStore.Source;

/// <summary>An entity type for namespacing events and verifying the type</summary>
public sealed class EntityType : ValueObject<EntityType>
{
    private readonly string name;

    /// <summary>Initialize a type</summary>
    /// <param name="name">The string value that uniquely identifies the type (and its events)</param>
    public EntityType(string name)
    {
        GuardIsValid(name);
        this.name = name;
    }

    protected override IEnumerable<object> GetEqualityComponents() => ImmutableArray.Create(name);

    public override string ToString() => name;

    private static void GuardIsValid(string name)
    {
        if (!Regex.IsMatch(name, "^[a-zA-Z0-9_-]+$"))
            throw new ArgumentOutOfRangeException(nameof(name), $"'{name}' is not a valid entity-type name");
    }
}
