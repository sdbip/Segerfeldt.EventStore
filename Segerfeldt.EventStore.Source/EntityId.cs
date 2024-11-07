using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Segerfeldt.EventStore.Source;

/// <summary>
/// An entity identifier.
///
/// An Entityid is essentially a <c cref="string">String</c> with validation rules.
/// You can use it wherever strings are accepted.
/// </summary>
[JsonConverter(typeof(EntityIdStringConverter))]
public sealed class EntityId : ValueObject<EntityId>
{
    private readonly string value;

    private EntityId(string value) => this.value = value;

    /// <summary>Initialize an identifier</summary>
    /// <param name="value">The string value that uniquely identifies the identity (and its events)</param>
    public static EntityId Value(string value)
    {
        if (!IsValidId(value)) throw new ArgumentOutOfRangeException(nameof(value), $"'{value}' is not a valid entity-id");
        return Safe(value);
    }

    /// <summary>Initialize an identifier</summary>
    /// <param name="value">The string value that uniquely identifies the identity (and its events)</param>
    public static EntityId? ValueOrNull(string? value)
    {
        if (value is null) return null;
        return Value(value);
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

    public TypedEntityId TypedWith(EntityType type) => new(this, type);

    /// <inheritdoc />
    protected override IEnumerable<object> GetEqualityComponents() => ImmutableArray.Create(value);

    // Implicit operator allows EntityId to be used where string is expected.
    public static implicit operator string(EntityId entityId) => entityId.value;
    /// <inheritdoc />
    public override string ToString() => value;

    #pragma warning disable SYSLIB1045 // Don't want partial classes
    private static bool IsValidId(string entityId) => Regex.IsMatch(entityId, "^[a-zA-Z0-9_-]+=*$");

    private class EntityIdStringConverter : JsonConverter<EntityId>
    {
        public override EntityId? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (typeToConvert != typeof(EntityId)) throw new ArgumentOutOfRangeException(nameof(typeToConvert), $"Unsupported type {typeToConvert}");
            if (reader.GetString() is not {} value) throw new Exception("Expected string value");
            return Value(value);
        }

        public override void Write(Utf8JsonWriter writer, EntityId value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
}
