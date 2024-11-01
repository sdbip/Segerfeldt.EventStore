using Segerfeldt.EventStore.Source;
using Segerfeldt.EventStore.Source.CommandAPI;

using SourceWebApplication.Domain;

namespace SourceWebApplication.Commands;

/// <summary>This summary is used to describe the generated endpoint as well as the command DTO.</summary>
public record RegisterUser(EntityId Username);

/// <inheritdoc/>
[AddsEntity("User")]
public sealed class RegisterUserCommandHandler : CommandHandlerBase<RegisterUser>
{
    /// <inheritdoc/>
    protected override async Task<CommandResult> Execute(RegisterUser command)
    {
        // Check for duplications.
        var username = command.Username;
        if (await EntityStore.ContainsEntityAsync(username))
            return CommandResult.Forbidden($"The username [{username}] is already in use");

        // Perform operation(s) related to this command.
        var user = User.New(username);

        // Publish the changes to the entity.
        await PublishChangesAsync(user);
        return CommandResult.NoContent();
    }
}
