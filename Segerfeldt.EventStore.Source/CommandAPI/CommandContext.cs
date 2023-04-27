using JetBrains.Annotations;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Segerfeldt.EventStore.Source.CommandAPI;

[PublicAPI]
public sealed class CommandContext
{
    public required HttpContext HttpContext { get; init; }
    public required IEntityStore EntityStore { get; init; }
    public required IEventPublisher EventPublisher { get; init; }

    public string GetRouteParameter(string name) => (string)HttpContext.GetRouteValue(name)!;

    public EntityId GetEntityIdParameter() => new(GetRouteParameter(ModifiesEntityAttribute.DefaultEntityId));
}
