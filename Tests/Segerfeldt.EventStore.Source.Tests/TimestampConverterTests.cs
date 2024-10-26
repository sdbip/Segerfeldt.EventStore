using Segerfeldt.EventStore.Source.Internals;

using System;

namespace Segerfeldt.EventStore.Source.Tests;

public class TimestampConverterTests
{
    [TestCase(1900, -25_567)]
    [TestCase(2000, 10_957)]
    public void FromDateTimeOffset(int year, double timestamp)
    {
        Assert.That(TimestampConverter.ToOADate(MidnightOnJanFirstUTC(year).ToOffset(TimeSpan.FromHours(3))), Is.EqualTo(timestamp).Within(1e-4));
    }

    [TestCase(-25_567, 1900)]
    [TestCase(10_957, 2000)]
    public void FromTimestampReturnsCorrectDateTime(double timestamp, int year)
    {
        Assert.That(TimestampConverter.ToDateTime(timestamp), Is.EqualTo(MidnightOnJanFirstUTC(year)));
    }

    private static DateTimeOffset MidnightOnJanFirstUTC(int year) => new(new DateTime(year, 1, 1), TimeSpan.Zero);
}
