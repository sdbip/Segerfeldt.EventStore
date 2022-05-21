using System;

namespace Segerfeldt.EventStore.Source.Internals;

public static class JulianDayConverter
{
    private const double OADateZero = 2_415_018.5; // Julian Date at midnight Dec 30, 1899

    public static double ToJulianDay(this DateTime date) => date.ToOADate() + OADateZero;

    public static DateTime FromJulianDay(double julianDay) => DateTime.FromOADate(julianDay - OADateZero);
}
