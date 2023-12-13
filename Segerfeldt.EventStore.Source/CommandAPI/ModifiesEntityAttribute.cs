using JetBrains.Annotations;

using Microsoft.OpenApi.Models;

using System;

namespace Segerfeldt.EventStore.Source.CommandAPI;

/// <summary>
/// Annotate a command handler to generate a RESTful HTTP endpoint that modifies a property on an entity.
/// Typical form: POST /entity/{entityId}/property
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
[PublicAPI, MeansImplicitUse]
public class ModifiesEntityAttribute : Attribute
{
    public const string DefaultEntityId = "entityId";

    /// <summary>
    /// The entity name to be used on the path of the generated endpoint.
    /// Example path: /entity/{entityId}
    /// </summary>
    public string Entity { get; }
    /// <summary>
    /// The HTTP method (verb) to be used for the generated endpoint.
    /// Default: POST
    /// </summary>
    public OperationType Method { get; set; }
    /// <summary>
    /// The name of the entity id path component.
    /// Default: entityId
    /// Example path: /entity/{entityId}
    /// </summary>
    public string? EntityId { get; init; }
    /// <summary>
    /// An optional property name to be used on the path of the generated endpoint.
    /// Example path: /entity/{entityId}/property
    /// </summary>
    public string? Property { get; init; }
    /// <summary>
    /// An optional property if to be used on the path of the generated endpoint.
    /// Example path: /entity/{entityId}/property/{propertyId}
    /// </summary>
    public string? PropertyId { get; init; }
    /// <summary>
    /// An optional property name to be used on the path of the generated endpoint.
    /// Example path: /entity/{entityId}/property/{propertyId}/subproperty
    /// </summary>
    public string? Subproperty { get; init; }

    internal string EntityIdOrDefault => EntityId ?? DefaultEntityId;
    internal bool HasEntityIdParameter => Method == OperationType.Delete || Property is not null;
    public bool HasPropertyIdParameter => PropertyId is not null;
    public bool HasSubpropertyParameter => Subproperty is not null;

    internal string Pattern =>
        Property is not null ? SpecificPropertyPattern :
        EntityId is not null ? SpecificEntityPattern :
        BaseEntityPattern;

    private string SpecificPropertyPattern => $"{SpecificEntityPattern}/{Property}{(PropertyId is null ? "" : $"/{{{PropertyId}}}")}{(Subproperty is null ? "" : $"/{Subproperty}")}";
    private string SpecificEntityPattern => $"{BaseEntityPattern}/{{{EntityIdOrDefault}}}";
    private string BaseEntityPattern => $"/{Entity.ToLowerInvariant()}";

    public ModifiesEntityAttribute(string entity) : this(entity, OperationType.Post) { }
    protected ModifiesEntityAttribute(string entity, OperationType method)
    {
        Entity = entity;
        Method = method;
    }
}
