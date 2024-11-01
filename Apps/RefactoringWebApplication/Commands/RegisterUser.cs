using RefactoringWebApplication.Domain;

using Segerfeldt.EventStore.Source;
using Segerfeldt.EventStore.Source.CommandAPI;

namespace RefactoringWebApplication.Commands;

/// <summary>This summary is used to describe the generated endpoint as well as the command DTO.</summary>
public record RegisterUser(string Username);

/// <inheritdoc/>
[AddsEntity("User")]
public sealed class RegisterUserCommandHandler : CommandHandlerBase<RegisterUser>
{
    /// <inheritdoc/>
    protected override async Task<CommandResult> Execute(RegisterUser command)
    {
        EntityId entityId;
        try { entityId = EntityId.Value(command.Username); }
        catch (ArgumentException exception) { return CommandResult.BadRequest(exception.Message); }

        // Check for duplications.
        if (EntityStore.ContainsEntity(entityId))
            return CommandResult.Forbidden($"The username [{entityId}] is already in use");

        // Perform operation(s) related to this command.
        var user = User.New(entityId);

        // Publish the changes to the entity.
        await PublishChangesAsync(user);
        return CommandResult.NoContent();
    }
}
