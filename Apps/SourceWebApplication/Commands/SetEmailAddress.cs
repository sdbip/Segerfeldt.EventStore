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
        // Get the identity of the authenticated user.
        var actor = context.HttpContext.User.Identity?.Name;
        // Return 401 UNAUTHORIZED if the user cnnot be idetified securely.
        if (actor is null) return CommandResult.Unauthorized();

        // Validate command properties.
        var emailAddress = EmailAddress.Of(command.EmailAddress);
        if (emailAddress.IsFailure) return CommandResult.BadRequest($"Invalid email address {command.EmailAddress}");

        // Retrieve the entities that matter for this command.
        var availability = await EmailAddressAvailability.GetAsync(context.EntityStore);
        var user = await context.EntityStore.ReconstituteAsync<User>(context.GetEntityId(), User.EntityType);
        if (user is null) return CommandResult.NotFound($"There is no user with username [{context.GetEntityId()}]");

        // Perform operation(s) related to this command.
        var result = availability.Claim(emailAddress.OrThrow());
        if (result.IsFailure) return CommandResult.Forbidden(result.Error);
        user.SetEmailAddress(emailAddress.OrThrow());

        // Publish all the changes in a single atomic operation.
        await context.EventPublisher.PublishChangesAsync([user, availability], actor);
        return CommandResult.NoContent();
    }
}
