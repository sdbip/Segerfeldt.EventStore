using JetBrains.Annotations;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using System.Security.Principal;

namespace Segerfeldt.EventStore.Source.CommandAPI.HTTPServices;

[PublicAPI]
public interface IRequest
{
    IPrincipal User { get; }
    object? GetRouteValue(string name);
}

[PublicAPI]
public class WrappedHttpContext : IRequest
{
    private readonly HttpContext httpContext;

    public IPrincipal User => httpContext.User;

    public WrappedHttpContext(HttpContext httpContext) => this.httpContext = httpContext;

    public object? GetRouteValue(string name) => httpContext.GetRouteValue(name);
}
