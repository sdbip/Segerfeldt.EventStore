using Segerfeldt.EventStore.Source;
using Segerfeldt.EventStore.Source.CommandAPI;

using SourceWebApplication.Domaim;

namespace SourceWebApplication.Commands;

/// <summary>This summary is used to describe the generated endpoint as well as the command DTO.</summary>
public record RegisterUser(string Username);

/// <inheritdoc/>
[AddsEntity("User")]
public class RegisterUserCommandHandler : ICommandHandler<RegisterUser>
{
    /// <inheritdoc/>
    public async Task<CommandResult> Handle(RegisterUser command, CommandContext context)
    {
        var entityId = new EntityId(command.Username);
        if (context.EntityStore.ContainsEntity(entityId))
            return CommandResult.Forbidden($"The username [{entityId}] is already in use");

        var user = User.New(entityId);
        await context.EventPublisher.PublishChangesAsync(user, "test_user");
        return CommandResult.NoContent();
    }
}
