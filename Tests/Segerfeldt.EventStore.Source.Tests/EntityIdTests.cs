using System;

namespace Segerfeldt.EventStore.Source.Tests;

public class EntityIdTests
{
    [Test]
    public void IsValidId(
        [Values(
            "lowercase",
            "UPPERCASE",
            "hyphen-",
            "underscore_",
            "numbers123"
        )]
    string id)
    {
        Assert.That(() => new EntityId(id), Throws.Nothing);
    }

    [Test]
    public void IsNotValidId(
        [Values(
            "this contains spaces",
            "{brace}",
            "no.dots.allowed",
            ""
        )] string id)
    {
        Assert.That(() => new EntityId(id), Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void IsImplicitlyConvertedToString()
    {
        Assert.That(() => IsValidId(new EntityId("id")), Throws.Nothing);
    }

    [Test]
    public void GuidIsValid(
        [Values(
            "b11ba185-7ea6-4654-b350-60e0c189683f",
            "a5fa1cd1-3c46-45f4-a2b7-6fae3b5ce0c1"
        )] string guid)
    {
        Assert.That(() => IsValidId(new EntityId(guid)), Throws.Nothing);
    }

    [Test]
    public void Base64EncodedGuidIsValid(
        [Values(
            "haEbsaZ-VEazUGDgwYloPw==",
            "0Rz6pUY89EWit2-uO1zgwQ==",
            "a9UE_UYuLE63M7MwiPNHrg=="
        )] string encoded)
    {
        Assert.That(() => IsValidId(new EntityId(encoded)), Throws.Nothing);
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
