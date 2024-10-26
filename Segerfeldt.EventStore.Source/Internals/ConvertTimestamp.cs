using System;

namespace Segerfeldt.EventStore.Source.Internals;

public static class ConvertTimestamp
{
    private const double OADateZero = -25_569; // Dec 30, 1899 is 25 549 days before the Unix Epoch

    public static double ToOADate(DateTimeOffset date) => date.UtcDateTime.ToOADate() + OADateZero;

    public static DateTimeOffset ToDateTime(double oaDate) => new(DateTime.FromOADate(oaDate - OADateZero), TimeSpan.Zero);
}
