using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Segerfeldt.EventStore.Source;

/// <summary>An ordinal for sorting events chronologically</summary>
[JsonConverter(typeof(OrdinalIntConverter))]
public sealed class Ordinal : ValueObject<Ordinal>, IComparable<Ordinal>
{
    /// <summary>The first ever published event</summary>
    public static Ordinal Zero => new(0);

    /// <summary>The value of the ordinal</summary>
    public int Value { get; }

    private Ordinal(int value) => Value = value;

    /// <summary>Create an <see cref="Ordinal"/></summary>
    /// <param name="value">the value of the ordinal (must be > 0)</param>
    /// <returns></returns>
    public static Ordinal Of(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(value));
        return Safe(value);
    }

    internal static Ordinal Safe(int value) => new(value);

    /// <inheritdoc />
    protected override IEnumerable<object> GetEqualityComponents() => [Value];

    internal Ordinal Next() => new(Value + 1);

    /// <inheritdoc />
    public override string ToString() => $"[{Value}]";

    public int CompareTo(Ordinal? other) => Value.CompareTo(other?.Value);

    private class OrdinalIntConverter : JsonConverter<Ordinal>
    {
        public override Ordinal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (typeToConvert != typeof(Ordinal)) throw new ArgumentOutOfRangeException(nameof(typeToConvert), $"Unsupported type {typeToConvert}");
            return Of(reader.GetInt32());
        }

        public override void Write(Utf8JsonWriter writer, Ordinal ordinal, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(ordinal.Value);
        }
    }
}
