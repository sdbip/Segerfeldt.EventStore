using JetBrains.Annotations;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Segerfeldt.EventStore.Source.CommandAPI;

[PublicAPI]
public sealed class CommandContext
{
    public HttpContext HttpContext { get; }
    public IEntityStore EntityStore { get; }
    public IEventPublisher EventPublisher { get; }

    public CommandContext(IEventPublisher eventPublisher, IEntityStore entityStore, HttpContext httpContext)
    {
        EventPublisher = eventPublisher;
        EntityStore = entityStore;
        HttpContext = httpContext;
    }

    public string GetRouteParameter(string name) => (string)HttpContext.GetRouteValue(name)!;

    public EntityId GetEntityIdParameter() => new(GetRouteParameter(ModifiesEntityAttribute.DefaultEntityId));
}
