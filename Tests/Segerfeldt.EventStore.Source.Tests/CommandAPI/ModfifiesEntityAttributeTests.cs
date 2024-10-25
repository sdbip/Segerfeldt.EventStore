using Microsoft.OpenApi.Models;

using Segerfeldt.EventStore.Source.CommandAPI;

namespace Segerfeldt.EventStore.Source.Tests.CommandAPI;

public sealed class ModifiesEntityAttributeTests
{
    [Test]
    public void DefaultsToPOST()
    {
        var attribute = new ModifiesEntityAttribute("Entity");

        Assert.That(attribute.MethodString, Is.EqualTo("POST"));
    }

    [Test]
    public void AllowsSettingTheMethod()
    {
        var attribute = new ModifiesEntityAttribute("Entity")
            { Method = OperationType.Get };

        Assert.That(attribute.MethodString, Is.EqualTo("GET"));
    }

    [Test]
    public void CustomMethodOverridesMethod()
    {
        var attribute = new ModifiesEntityAttribute("Entity")
            { CustomMethod = "DOSTUFF" };

        Assert.That(attribute.MethodString, Is.EqualTo("DOSTUFF"));
    }

    [Test]
    public void SettingEntityIdExtendsPattern()
    {
        var attribute = new ModifiesEntityAttribute("Entity")
            { EntityId = ModifiesEntityAttribute.DefaultEntityId, };

        Assert.That(attribute.Pattern, Is.EqualTo("/entity/{entityId}"));
    }

    [Test]
    public void DeleteSubclassSetsPattern()
    {
        var attribute = new DeletesEntityAttribute("Entity");

        Assert.That(attribute.Pattern, Is.EqualTo("/entity/{entityId}"));
    }

    [Test]
    public void SettingAddEntityIdExtendsPattern()
    {
        var attribute = new ModifiesEntityAttribute("Entity")
            { IncludeEntityId = true, };

        Assert.That(attribute.Pattern, Is.EqualTo("/entity/{entityId}"));
    }

    [Test]
    public void SettingPropertyExtendsPattern()
    {
        var attribute = new ModifiesEntityAttribute("Entity")
            { Property = "property", };

        Assert.That(attribute.Pattern, Is.EqualTo("/entity/{entityId}/property"));
    }

    [Test]
    public void SettingPropertyIdExtendsPattern()
    {
        var attribute = new ModifiesEntityAttribute("Entity")
        {
            Property = "property",
            PropertyId = "propertyId",
        };

        Assert.That(attribute.Pattern, Is.EqualTo("/entity/{entityId}/property/{propertyId}"));
    }

    [Test]
    public void PropertyIdWithoutPropertyIsIgnored()
    {
        var attribute = new ModifiesEntityAttribute("Entity")
            { PropertyId = "property", };

        Assert.That(attribute.Pattern, Is.EqualTo("/entity/{entityId}"));
    }

    [Test]
    public void AddsEntityOmitsEntityId()
    {
        var attribute = new AddsEntityAttribute("Entity")
            { PropertyId = "property", };

        Assert.That(attribute.Pattern, Is.EqualTo("/entity"));
    }

    [Test]
    public void SettingSubpropertyExtendsPattern()
    {
        var attribute = new ModifiesEntityAttribute("Entity")
        {
            Property = "property",
            PropertyId = "propertyId",
            Subproperty = "subproperty",
        };

        Assert.That(attribute.Pattern, Is.EqualTo("/entity/{entityId}/property/{propertyId}/subproperty"));
    }
}
