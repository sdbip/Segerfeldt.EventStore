using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Segerfeldt.EventStore.Source;

/// <summary>
/// An entity type for namespacing events and verifying the type.
/// 
/// An EntityType is essentially a <c cref="string">String</c> with validation rules.
/// You can use it wherever strings are accepted.
/// </summary>
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

    public static implicit operator string(EntityType type) => type.name;
    public override string ToString() => name;

    private static void GuardIsValid(string name, [CallerArgumentExpression(nameof(name))] string? parameterName = null)
    {
        if (!IsValidTypeName(name))
            throw new ArgumentOutOfRangeException(parameterName, $"'{name}' is not a valid entity-type name");
    }

    #pragma warning disable SYSLIB1045 // Avoid partial classes
    private static bool IsValidTypeName(string name) => Regex.IsMatch(name, "^[a-zA-Z0-9._-]+$");
}
