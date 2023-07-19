using NUnit.Framework;

using System;

namespace Segerfeldt.EventStore.Source.Tests;

public class EntityIdTests
{
    [TestCase("lowercase")]
    [TestCase("UPPERCASE")]
    [TestCase("hyphen-")]
    [TestCase("underscore_")]
    [TestCase("numbers123")]
    public void IsValidId(string id)
    {
        Assert.That(() => new EntityId(id), Throws.Nothing);
    }

    [TestCase("this contains spaces")]
    [TestCase("{brace}")]
    [TestCase("no.dots.allowed")]
    [TestCase("")]
    public void IsNotValidId(string id)
    {
        Assert.That(() => new EntityId(id), Throws.InstanceOf<ArgumentOutOfRangeException>());
    }
}
