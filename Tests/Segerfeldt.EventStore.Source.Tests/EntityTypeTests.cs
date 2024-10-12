using System;

namespace Segerfeldt.EventStore.Source.Tests;

public sealed class EntityTypeTests
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
        Assert.That(() => EntityType.Name(name).OrThrow(), Throws.Nothing);
    }

    [Test]
    public void IsNotValidName(
        [Values(
            "this contains spaces",
            "{brace}",
            ""
        )] string name)
    {
        Assert.That(() => EntityType.Name(name).OrThrow(), Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void IsImplicitlyConvertedToString()
    {
        Assert.That(() => IsValidType(EntityType.Name("name").OrThrow()), Throws.Nothing);
    }
}
