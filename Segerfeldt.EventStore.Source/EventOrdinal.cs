using System;
using System.Collections.Generic;

namespace Segerfeldt.EventStore.Source;

public sealed class EventOrdinal : ValueObject<EventOrdinal>
{
    public static EventOrdinal Never => new(-1);
    public static EventOrdinal Zero => new(0);

    public int Value { get; }

    private EventOrdinal(int value) => Value = value;

    public static EventOrdinal Of(int value)
    {
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "Must be positive");
        return new(value);
    }

    protected override IEnumerable<object> GetEqualityComponents() => [Value];

    internal EventOrdinal Next() => new(Value + 1);

    public override string ToString() => $"[{Value}]";
}
