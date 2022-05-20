using System;

namespace Segerfeldt.EventStore.Source.Tests.Extensions;

internal static class DateTimeExtension
{
    public static double ToJulianDate(this DateTime dateTime)
    {
        var seconds = dateTime.Ticks / 10_000_000.0;
        var days = seconds / 86_400;
        return days + 1721423.5;
    }
}
