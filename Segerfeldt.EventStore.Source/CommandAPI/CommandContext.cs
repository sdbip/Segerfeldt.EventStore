using JetBrains.Annotations;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Segerfeldt.EventStore.Source.CommandAPI;

/// <summary>Container for tools and information when handling a command</summary>
[PublicAPI]
public sealed class CommandContext
{
    /// <summary>HTTP-specific information about the HTTP request</summary>
    public required HttpContext HttpContext { get; init; }
    /// <summary>An object used to reconstitute entities</summary>
    public required EntityStore EntityStore { get; init; }
    /// <summary>An object used to publish the changes to the reconxtituted entities</summary>
    public required EventPublisher EventPublisher { get; init; }

    /// <summary>Gets the value of a route parameter (named placeholders in the route pattern)</summary>
    /// <param name="name">the placeholder name</param>
    public object? GetRouteParameter(string name) => HttpContext.GetRouteValue(name)!;

    /// <summary>Gets the value of the entity-id parameter from the URL</summary>
    /// <param name="parameterName">
    /// The placeholder name as set in the handler's <see cref="ModifiesEntityAttribute.EntityId"/> property.
    /// Omit the parameter if the the property is omitted.
    /// </param>
    public EntityId GetEntityId(string parameterName = ModifiesEntityAttribute.DefaultEntityId) =>
        EntityId.Safe((string)GetRouteParameter(parameterName)!);
}
