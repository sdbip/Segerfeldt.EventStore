using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace Segerfeldt.EventStore.Source;

/// <summary>
/// An entity identifier.
///
/// An Entityid is essentially a <c cref="string">String</c> with validation rules.
/// You can use it wherever strings are accepted.
/// </summary>
public sealed class EntityId : ValueObject<EntityId>
{
    private readonly string value;

    private EntityId(string value) => this.value = value;

    /// <summary>Initialize an identifier</summary>
    /// <param name="value">The string value that uniquely identifies the identity (and its events)</param>
    public static Result<EntityId> Value(string value)
    {
        if (!IsValidId(value)) return Failure.Error(new ArgumentOutOfRangeException(nameof(value), $"'{value}' is not a valid entity-id"));
        return Safe(value);
    }

    internal static EntityId Safe(string value) => new(value);

    /// <summary>Generates a new EntityId as a 36 characters long GUID string</summaryz>
    /// <returns>a generated EntityId</returns>
    public static EntityId NewGuid()
    {
        var guid = Guid.NewGuid();
        return new EntityId(guid.ToString());
    }

    /// <summary>Generates a new EntityId as a 24 characters long Base64 (URL) encoded GUID</summaryz>
    /// <returns>a generated EntityId</returns>
    public static EntityId NewBase64Guid()
    {
        var guid = Guid.NewGuid();
        return new EntityId(Convert.ToBase64String(guid.ToByteArray()).Replace('+', '-').Replace("/", "_"));
    }

    /// <inheritdoc />
    protected override IEnumerable<object> GetEqualityComponents() => ImmutableArray.Create(value);

    // Implicit operator allows EntityId to be used where string is expected.
    public static implicit operator string(EntityId entityId) => entityId.value;
    /// <inheritdoc />
    public override string ToString() => value;

    #pragma warning disable SYSLIB1045 // Don't want partial classes
    private static bool IsValidId(string entityId) => Regex.IsMatch(entityId, "^[a-zA-Z0-9_-]+=*$");
}
