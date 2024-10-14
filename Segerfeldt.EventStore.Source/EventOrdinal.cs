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
    public static Result<EventOrdinal> Of(int value)
    {
        if (value < 0) return Failure.Error(new ArgumentOutOfRangeException(nameof(value), "Must be positive"));
        return Safe(value);
    }

    internal static EventOrdinal Safe(int value) => new(value);

    /// <inheritdoc />
    protected override IEnumerable<object> GetEqualityComponents() => [Value];

    internal EventOrdinal Next() => new(Value + 1);

    /// <inheritdoc />
    public override string ToString() => $"[{Value}]";
}
