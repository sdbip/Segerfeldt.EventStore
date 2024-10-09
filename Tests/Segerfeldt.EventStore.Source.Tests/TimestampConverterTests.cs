using Segerfeldt.EventStore.Source.Internals;

using System;

namespace Segerfeldt.EventStore.Source.Tests;

public class TimestampConverterTests
{
    [TestCase(1900, -25_567)]
    [TestCase(2000, 10_957)]
    public void NewYearMidnights(int year, double julianDay)
    {
        Assert.That(new DateTime(year, 1, 1).DaysSinceEpoch(), Is.EqualTo(julianDay).Within(1e-4));
    }
}
