using System;

namespace Segerfeldt.EventStore.Source.Tests;

public sealed class TimestampTests
{
    [TestCase(1970, 0)]
    [TestCase(2000, 10_957)]
    public void FromDateTimeOffset(int year, double timestamp)
    {
        Assert.That(Timestamp.FromDateTime(MidnightOnJanFirstUTC(year).ToOffset(TimeSpan.FromHours(3))).Value, Is.EqualTo(timestamp).Within(1e-4));
    }

    [TestCase(0, 1970)]
    [TestCase(10_957, 2000)]
    public void FromTimestampReturnsCorrectDateTime(double timestamp, int year)
    {
        Assert.That(Timestamp.DaysSinceUnixEpoch(timestamp).UTCDateTime, Is.EqualTo(MidnightOnJanFirstUTC(year)));
    }

    [Test]
    public void ThrowsIfBeforeEpoch()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => Timestamp.DaysSinceUnixEpoch(-1), Throws.Exception);
            Assert.That(() => Timestamp.FromDateTime(MidnightOnJanFirstUTC(1960)), Throws.Exception);
        });
    }

    private static DateTimeOffset MidnightOnJanFirstUTC(int year) => new(new DateTime(year, 1, 1), TimeSpan.Zero);
}
