using System;
using System.Collections.Generic;

namespace Segerfeldt.EventStore.Source;

/// <summary>An ordinal for sorting events chronologically</summary>
public sealed class EventOrdinal : ValueObject<EventOrdinal>
{
    /// <summary>No event has been published yet</summary>
    public static EventOrdinal Never => new(-1);
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
        return value >= 0
            ? (Result<EventOrdinal>)Safe(value)
            : new Result<EventOrdinal>(null, new ArgumentOutOfRangeException(nameof(value), "Must be positive"));
    }

    internal static EventOrdinal Safe(int value) => new(value);

    protected override IEnumerable<object> GetEqualityComponents() => [Value];

    internal EventOrdinal Next() => new(Value + 1);

    public override string ToString() => Value < 0 ? "[Never]" : $"[{Value}]";
}
