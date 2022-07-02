using System;

namespace Segerfeldt.EventStore.Source.Internals;

public static class TimestampConverter
{
    private const double OADateZero = -25_569; // Dec 30, 1899 is 25 549 days before the Unix Epoch

    public static double DaysSinceEpoch(this DateTime date) => date.ToOADate() + OADateZero;

    public static DateTime FromTimestamp(double timestamp) => DateTime.FromOADate(timestamp - OADateZero);
}
