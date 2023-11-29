using JetBrains.Annotations;

using Microsoft.AspNetCore.Http;

using Segerfeldt.EventStore.Source.CommandAPI.HTTPServices;

namespace Segerfeldt.EventStore.Source.CommandAPI;

[PublicAPI]
public sealed class CommandContext
{
    public required HttpContext HttpContext { get; init; }
    public required IRequest Request { get; init; }
    public required IEntityStore EntityStore { get; init; }
    public required IEventPublisher EventPublisher { get; init; }

    public string GetRouteParameter(string name) => (string)Request.GetRouteValue(name)!;
}
