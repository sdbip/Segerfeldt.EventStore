using NUnit.Framework;

using System;

namespace Segerfeldt.EventStore.Source.Tests;

public class EntityTypeTests
{
    [TestCase("lowercase")]
    [TestCase("UPPERCASE")]
    [TestCase("hyphen-")]
    [TestCase("underscore_")]
    [TestCase("numbers123")]
    public void IsValidId(string id)
    {
        Assert.That(() => new EntityType(id), Throws.Nothing);
    }

    [TestCase("this contains spaces")]
    [TestCase("{brace}")]
    [TestCase("no.dots.allowed")]
    [TestCase("")]
    public void IsNotValidId(string id)
    {
        Assert.That(() => new EntityType(id), Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void IsImplicitlyConvertedToString()
    {
        Assert.That(() => IsValidId(new EntityType("name")), Throws.Nothing);
    }
}
