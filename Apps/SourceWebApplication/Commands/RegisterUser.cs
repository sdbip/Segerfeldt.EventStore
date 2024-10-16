using Segerfeldt.EventStore.Source;
using Segerfeldt.EventStore.Source.CommandAPI;

using SourceWebApplication.Domain;

namespace SourceWebApplication.Commands;

/// <summary>This summary is used to describe the generated endpoint as well as the command DTO.</summary>
public record RegisterUser(string Username);

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

        EntityId entityId;
        try { entityId = EntityId.Value(command.Username); }
        catch (ArgumentException exception) { return CommandResult.BadRequest(exception.Message); }

        // Check for duplications.
        if (context.EntityStore.ContainsEntity(entityId))
            return CommandResult.Forbidden($"The username [{entityId}] is already in use");

        // Perform operation(s) related to this command.
        var user = User.New(entityId);

        // Publish the changes to the entity.
        await context.EventPublisher.PublishChangesAsync(user, actor);
        return CommandResult.NoContent();
    }
}
