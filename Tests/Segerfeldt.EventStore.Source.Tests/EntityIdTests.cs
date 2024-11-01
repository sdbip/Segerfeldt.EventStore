using System;

namespace Segerfeldt.EventStore.Source.Tests;

public sealed class EntityIdTests
{
    [TestCase("lowercase")]
    [TestCase("UPPERCASE")]
    [TestCase("hyphen-")]
    [TestCase("underscore_")]
    [TestCase("numbers123")]
    public void IsValidId(string id)
    {
        Assert.That(() => EntityId.Value(id), Throws.Nothing);
    }

    [TestCase("this contains spaces")]
    [TestCase("{brace}")]
    [TestCase("no.dots.allowed")]
    [TestCase("")]
    public void IsNotValidId(string id)
    {
        Assert.That(() => EntityId.Value(id), Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void IsImplicitlyConvertedToString()
    {
        Assert.That(() => IsValidId(EntityId.Value("id")), Throws.Nothing);
    }

    [TestCase("b11ba185-7ea6-4654-b350-60e0c189683f")]
    [TestCase("a5fa1cd1-3c46-45f4-a2b7-6fae3b5ce0c1")]
    public void GuidIsValid(string guid)
    {
        Assert.That(() => EntityId.Value(guid), Throws.Nothing);
    }

    [TestCase("haEbsaZ-VEazUGDgwYloPw==")]
    [TestCase("0Rz6pUY89EWit2-uO1zgwQ==")]
    [TestCase("a9UE_UYuLE63M7MwiPNHrg==")]
    public void Base64EncodedGuidIsValid(string encoded)
    {
        Assert.That(() => IsValidId(EntityId.Value(encoded)), Throws.Nothing);
    }

    [Test]
    public void CanGenerateNewGuid()
    {
        var entityId = EntityId.NewGuid();
        Assert.That(() => Guid.Parse(entityId), Throws.Nothing);
    }

    [Test]
    public void CanGenerateNewBase64EncodedGuid()
    {
        var entityId = EntityId.NewBase64Guid().ToString();
        var normalizedBase64String = entityId.Replace('-', '+').Replace("_", "/");
        Assert.That(() => new Guid(Convert.FromBase64String(normalizedBase64String)), Throws.Nothing);
    }
}
