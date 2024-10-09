using System;

namespace Segerfeldt.EventStore.Source.Tests;

public class EntityTypeTests
{
    [Test]
    public void IsValidType(
        [Values(
            "lowercase",
            "UPPERCASE",
            "hyphen-",
            "underscore_",
            "numbers123",
            "dots.are.allowed"
        )] string name)
    {
        Assert.That(() => new EntityType(name), Throws.Nothing);
    }

    [Test]
    public void IsNotValidName(
        [Values(
            "this contains spaces",
            "{brace}",
            ""
        )] string name)
    {
        Assert.That(() => new EntityType(name), Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void IsImplicitlyConvertedToString()
    {
        Assert.That(() => IsValidType(new EntityType("name")), Throws.Nothing);
    }
}
