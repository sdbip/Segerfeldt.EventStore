using NUnit.Framework;

using Segerfeldt.EventStore.Source.Internals;

using System;

namespace Segerfeldt.EventStore.Source.Tests;

public class JulianDayTests
{
    [TestCase(1900, 2415020.5)]
    [TestCase(2000, 2451544.5)]
    public void NewYearMidnights(int year, double julianDay)
    {
        Assert.That(new DateTime(year, 1, 1).ToJulianDay(), Is.EqualTo(julianDay).Within(1e-4));
    }
}
