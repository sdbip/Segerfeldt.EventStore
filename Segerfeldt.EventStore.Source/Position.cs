using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Segerfeldt.EventStore.Source;

/// <summary>An ordinal for sorting events chronologically</summary>
[JsonConverter(typeof(PositionLongConverter))]
public sealed class Position : ValueObject<Position>, IComparable<Position>
{
    /// <summary>The first ever published event</summary>
    public static Position Zero => new(0);

    /// <summary>The value of the ordinal</summary>
    public long Value { get; }

    private Position(long value) => Value = value;

    /// <summary>Create an <see cref="Position"/></summary>
    /// <param name="value">the value of the ordinal (must be > 0)</param>
    /// <returns></returns>
    public static Position Of(long value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(value));
        return Safe(value);
    }

    internal static Position Safe(long value) => new(value);

    /// <inheritdoc />
    protected override IEnumerable<object> GetEqualityComponents() => [Value];

    internal Position Next() => new(Value + 1);

    /// <inheritdoc />
    public override string ToString() => $"[{Value}]";

    public int CompareTo(Position? other) => Value.CompareTo(other?.Value);

    private class PositionLongConverter : JsonConverter<Position>
    {
        public override Position? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (typeToConvert != typeof(Position)) throw new ArgumentOutOfRangeException(nameof(typeToConvert), $"Unsupported type {typeToConvert}");
            return Safe(reader.GetInt64());
        }

        public override void Write(Utf8JsonWriter writer, Position value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value.Value);
        }
    }
}
