using System.Net.Http.Headers;
using System.Security.Principal;

using Microsoft.AspNetCore.Authentication;

namespace SourceWebApplication;

/// <inheritdoc />
public class NaiveAuthenticationHandler : IAuthenticationHandler
{
    private HttpContext context = null!;

    /// <inheritdoc />
    public Task<AuthenticateResult> AuthenticateAsync() => Task.FromResult(Authenticate());

    private AuthenticateResult Authenticate()
    {
        var username = GetUsername();
        if (username is null) return AuthenticateResult.Fail("Missing Username authorization header.");

        var principal = new GenericPrincipal(new GenericIdentity(username), Array.Empty<string>());
        return AuthenticateResult.Success(new AuthenticationTicket(principal, "Trusting"));
    }

    private string? GetUsername()
    {
        var values = context.Request.Headers.Authorization.Select(x => x is null ? null : AuthenticationHeaderValue.Parse(x));
        return values.FirstOrDefault(v => v?.Scheme == "Username")?.Parameter;
    }

    /// <inheritdoc />
    public Task ChallengeAsync(AuthenticationProperties? properties) => Task.CompletedTask;

    /// <inheritdoc />
    public Task ForbidAsync(AuthenticationProperties? properties) => Task.CompletedTask;

    /// <inheritdoc />
    public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
    {
        this.context = context;
        return Task.CompletedTask;
    }
}
