using System;
using System.Collections.Generic;

namespace Segerfeldt.EventStore.Source;

/// <summary>An ordinal for sorting events chronologically</summary>
public sealed class EventOrdinal : ValueObject<EventOrdinal>
{
    /// <summary>The first ever published event</summary>
    public static EventOrdinal Zero => new(0);

    /// <summary>The value of the ordinal</summary>
    public int Value { get; }

    private EventOrdinal(int value) => Value = value;

    /// <summary>Create an <see cref="EventOrdinal"/></summary>
    /// <param name="value">the value of the ordinal (must be > 0)</param>
    /// <returns></returns>
    public static EventOrdinal Of(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(value));
        return Safe(value);
    }

    internal static EventOrdinal Safe(int value) => new(value);

    /// <inheritdoc />
    protected override IEnumerable<object> GetEqualityComponents() => [Value];

    internal EventOrdinal Next() => new(Value + 1);

    /// <inheritdoc />
    public override string ToString() => $"[{Value}]";
}
