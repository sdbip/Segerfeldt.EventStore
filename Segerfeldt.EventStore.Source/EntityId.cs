using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
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

    /// <summary>Initialize an identifier</summary>
    /// <param name="value">The string value that uniquely identifies the identity (and its events)</param>
    public EntityId(string value)
    {
        GuardIsValid(value);
        this.value = value;
    }

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

    protected override IEnumerable<object> GetEqualityComponents() => ImmutableArray.Create(value);

    // Implicit operator allows EntityId to be used where string is expected.
    public static implicit operator string(EntityId entityId) => entityId.value;
    public override string ToString() => value;


    private static void GuardIsValid(string entityId, [CallerArgumentExpression(nameof(entityId))] string? parameterName = null)
    {
        if (!IsValidId(entityId))
            throw new ArgumentOutOfRangeException(parameterName, $"'{entityId}' is not a valid entity-type name");
    }

    #pragma warning disable SYSLIB1045 // Don't want partial classes

    private static bool IsValidId(string entityId) => Regex.IsMatch(entityId, "^[a-zA-Z0-9_-]+=*$");
}
