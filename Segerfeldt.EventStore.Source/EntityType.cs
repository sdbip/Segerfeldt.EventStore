using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace Segerfeldt.EventStore.Source;

/// <summary>An entity type for namespacing events and verifying the type.</summary>
public sealed class EntityType : ValueObject<EntityType>
{
    private readonly string name;

    private EntityType(string name) => this.name = name;

    /// <summary>Initialize a type</summary>
    /// <param name="name">The string value that uniquely identifies the type (and its events)</param>
    public static Result<EntityType> Name(string name)
    {
        if (!IsValidTypeName(name)) return Failure.Error(new ArgumentOutOfRangeException(nameof(name), $"'{name}' is not a valid entity-id"));
        return Safe(name);
    }

    internal static EntityType Safe(string name) => new(name);

    /// <inheritdoc />
    protected override IEnumerable<object> GetEqualityComponents() => ImmutableArray.Create(name);

    public static implicit operator string(EntityType type) => type.name;
    /// <inheritdoc />
    public override string ToString() => name;

#pragma warning disable SYSLIB1045 // Avoid partial classes
    private static bool IsValidTypeName(string name) => Regex.IsMatch(name, "^[a-zA-Z0-9._-]+$");
}
