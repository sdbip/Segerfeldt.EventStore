using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace Segerfeldt.EventStore.Source;

/// <summary>And entity identifier</summary>
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

    protected override IEnumerable<object> GetEqualityComponents() => ImmutableArray.Create(value);

    public static implicit operator string(EntityId entityId) => entityId.value;
    public override string ToString() => value;

    private static void GuardIsValid(string entityId)
    {
        if (!Regex.IsMatch(entityId, "^[a-zA-Z0-9_-]+$"))
            throw new ArgumentOutOfRangeException(nameof(entityId), $"'{entityId}' is not a valid entity-type name");
    }
}
