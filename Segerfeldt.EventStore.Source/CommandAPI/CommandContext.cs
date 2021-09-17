using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Segerfeldt.EventStore.Source.CommandAPI
{
    public sealed class CommandContext
    {
        public HttpContext HttpContext { get; }
        public EntityStore EntityStore => HttpContext.RequestServices.GetRequiredService<EntityStore>();
        public EventPublisher EventPublisher => HttpContext.RequestServices.GetRequiredService<EventPublisher>();

        public CommandContext(HttpContext httpContext) => HttpContext = httpContext;

        public string GetRouteParameter(string name) => (string)HttpContext.GetRouteValue(name)!;

        public EntityId GetEntityIdParameter() => new(GetRouteParameter(ModifiesEntityAttribute.DefaultEntityId));
    }
}
