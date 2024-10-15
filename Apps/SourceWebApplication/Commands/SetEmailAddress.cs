using Segerfeldt.EventStore.Source;
using Segerfeldt.EventStore.Source.CommandAPI;

using SourceWebApplication.Domaim;

namespace SourceWebApplication.Commands;

/// <summary>This summary is used to describe the generated endpoint as well as the command DTO.</summary>
public record SetEmailAddress(string EmailAddress);

/// <inheritdoc/>
[ModifiesEntity("User", Property = "emailAddress")]
public sealed class SetEmailAddressCommandHandler : ICommandHandler<SetEmailAddress, string?>
{
    /// <inheritdoc/>
    public async Task<CommandResult<string?>> Handle(SetEmailAddress command, CommandContext context)
    {
        var actor = context.HttpContext.User.Identity?.Name;
        if (actor is null) return CommandResult.Unauthorized();

        var emailAddress = command.EmailAddress;
        var availability = await EmailAddressAvailability.GetAsync(context.EntityStore);

        var result = availability.Claim(emailAddress);
        if (result.IsFailure) return CommandResult.Forbidden(result.Error);

        var user = await context.EntityStore.ReconstituteAsync<User>(context.GetEntityId(), User.EntityType);
        if (user is null) return CommandResult.NotFound($"There is no user with username [{context.GetEntityId()}]");

        user.SetEmailAddress(emailAddress);

        await context.EventPublisher.PublishChangesAsync(user, actor);
        await context.EventPublisher.PublishChangesAsync(availability, actor);
        return CommandResult.NoContent();
    }
}
