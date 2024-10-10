using JetBrains.Annotations;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Segerfeldt.EventStore.Source.CommandAPI;

[PublicAPI]
public sealed class CommandContext
{
    public required HttpContext HttpContext { get; init; }
    public required EntityStore EntityStore { get; init; }
    public required EventPublisher EventPublisher { get; init; }

    public string GetRouteParameter(string name) => (string)HttpContext.GetRouteValue(name)!;
}
