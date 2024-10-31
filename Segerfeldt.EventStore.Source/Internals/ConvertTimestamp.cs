using System;

namespace Segerfeldt.EventStore.Source.Internals;

public static class ConvertTimestamp
{
    private const double TimestampAtOADateZero = -25_569; // Dec 30, 1899 is 25,549 days before the Unix Epoch

    public static double FromDateTime(DateTimeOffset date) => date.UtcDateTime.ToOADate() + TimestampAtOADateZero;

    public static DateTimeOffset ToDateTime(double timestamp) => new(DateTime.FromOADate(timestamp - TimestampAtOADateZero), TimeSpan.Zero);
}
