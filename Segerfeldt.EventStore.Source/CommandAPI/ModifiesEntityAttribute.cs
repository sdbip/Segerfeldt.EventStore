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
    /// This overrides the <see cref="Method"/> property.
    /// </summary>
    /// NOTE: This command will not be documented; custom methods are not supported by Swagger.
    /// Motivation here: https://github.com/domaindrivendev/Swashbuckle.WebApi/issues/429
    public string? CustomMethod { get; set; }
    /// <summary>
    /// The HTTP method (verb) to be used for the generated endpoint.
    /// Default: <see cref="OperationType.Post" />
    /// </summary>
    public OperationType Method { get; set; }
    /// <summary>The mode of command serialization</summary>
    public CommandSerializationMode SerializationType{ get; set; } = CommandSerializationMode.JSONBody;
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
    /// <summary>
    /// Flag that adds the default entity-id parameter to the <see cref="Pattern"/>
    /// even if no property has been added
    /// </summary>
    public bool IncludeEntityId { get; init; } = true;

    internal string EntityIdOrDefault => EntityId ?? DefaultEntityId;
    internal bool HasEntityIdParameter => IncludeEntityId || EntityId is not null || Property is not null;
    internal bool HasPropertyIdParameter => PropertyId is not null;
    internal bool HasSubpropertyParameter => Subproperty is not null;

    /// <summary>The actuaal HTTP method/verb (string) to use in requests</summary>
    public string MethodString => CustomMethod ?? Method.ToString().ToUpper();

    /// <summary>The pattern to match to URL paths when resolving the command hsndler</summary>
    public string Pattern =>
        Property is not null ? SpecificPropertyPattern :
        EntityId is not null || IncludeEntityId ? SpecificEntityPattern :
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

/// <summary>The mode of command serialization</summary>
public enum CommandSerializationMode
{
    /// <summary>The command will be added to the URL</summary>
    URLQuery,
    /// <summary>The command will be serialized as JSON and sent in the request body</summary>
    JSONBody
}
