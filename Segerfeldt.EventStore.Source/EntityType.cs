using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Segerfeldt.EventStore.Source;

/// <summary>An entity type for namespacing events and verifying the type.</summary>
[JsonConverter(typeof(EntityTypeStringConverter))]
public sealed class EntityType : ValueObject<EntityType>
{
    private readonly string name;

    private EntityType(string name) => this.name = name;

    /// <summary>Initialize a type</summary>
    /// <param name="name">The string value that uniquely identifies the type (and its events)</param>
    public static EntityType Name(string name)
    {
        if (!IsValidTypeName(name)) throw new ArgumentOutOfRangeException(nameof(name), $"'{name}' is not a valid entity-type");
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

    private class EntityTypeStringConverter : JsonConverter<EntityType>
    {
        public override EntityType? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (typeToConvert != typeof(EntityType)) throw new ArgumentOutOfRangeException(nameof(typeToConvert), $"Unsupported type {typeToConvert}");
            if (reader.GetString() is not {} value) throw new Exception("Expected string value");
            return Name(value);
        }

        public override void Write(Utf8JsonWriter writer, EntityType value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
}
