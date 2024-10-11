using System;
using System.Collections.Generic;

namespace Segerfeldt.EventStore.Source;

internal sealed class EventOrdinal : ValueObject<EventOrdinal>
{
    public static EventOrdinal Zero => new(0);

    public int Value { get; }

    public EventOrdinal(int value)
    {
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "Must be positive");
        Value = value;
    }

    protected override IEnumerable<object> GetEqualityComponents() => [Value];

    internal EventOrdinal Next() => new(Value + 1);

    public override string ToString() => $"[{Value}]";
}
