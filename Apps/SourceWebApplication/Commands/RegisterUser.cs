using Segerfeldt.EventStore.Source;
using Segerfeldt.EventStore.Source.CommandAPI;

using SourceWebApplication.Domain;

namespace SourceWebApplication.Commands;

/// <summary>This summary is used to describe the generated endpoint as well as the command DTO.</summary>
public record RegisterUser(EntityId Username);

/// <inheritdoc/>
[AddsEntity("User")]
public sealed class RegisterUserCommandHandler : ICommandHandler<RegisterUser>
{
    /// <inheritdoc/>
    public async Task<CommandResult> Handle(RegisterUser command, CommandContext context)
    {
        // Get the identity of the authenticated user.
        var actor = context.HttpContext.User.Identity?.Name;
        // Return 401 UNAUTHORIZED if the user cnnot be idetified securely.
        if (actor is null) return CommandResult.Unauthorized();

        // Check for duplications.
        var username = command.Username;
        if (context.EntityStore.ContainsEntity(username))
            return CommandResult.Forbidden($"The username [{username}] is already in use");

        // Perform operation(s) related to this command.
        var user = User.New(username);

        // Publish the changes to the entity.
        await context.EventPublisher.PublishChangesAsync(user, actor);
        return CommandResult.NoContent();
    }
}
