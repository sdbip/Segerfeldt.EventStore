using System;
using System.Collections.Generic;

namespace Segerfeldt.EventStore.Source;

public class Timestamp : ValueObject<Timestamp>
{
    private const double TimestampAtOADateZero = -25_569; // OADate Epoch is 25,549 days before the Unix Epoch

    public static Timestamp UnixEpoch => new(0);

    public double Value { get; }
    public DateTimeOffset UTCDateTime => new(DateTime.FromOADate(Value - TimestampAtOADateZero), TimeSpan.Zero);

    private Timestamp(double value) => Value = value;
    public static Timestamp FromDateTime(DateTimeOffset date) => DaysSinceUnixEpoch(date.UtcDateTime.ToOADate() + TimestampAtOADateZero);
    public static Timestamp DaysSinceUnixEpoch(double value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(value));
        return new(value);
    }

    protected override IEnumerable<object> GetEqualityComponents() => [Value];
}
