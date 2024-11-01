using RefactoringWebApplication.Domain;

using Segerfeldt.EventStore.Source;
using Segerfeldt.EventStore.Source.CommandAPI;

namespace RefactoringWebApplication.Commands;

/// <summary>This summary is used to describe the generated endpoint as well as the command DTO.</summary>
public record SetEmailAddress(string EmailAddress);

/// <inheritdoc/>
[ModifiesEntity("User", Property = "emailAddress")]
public sealed class SetEmailAddressCommandHandler : CommandHandlerBase<SetEmailAddress, string>
{
    /// <inheritdoc/>
    protected override async Task<CommandResult<string>> Execute(SetEmailAddress command)
    {
        // Validate command properties.
        EmailAddress emailAddress;
        try { emailAddress = EmailAddress.Of(command.EmailAddress); }
        catch (ArgumentOutOfRangeException exeption) { return CommandResult.BadRequest(exeption.Message); }

        // Retrieve the entities that matter for this command.
        var availability = await EmailAddressAvailability.GetAsync(EntityStore);
        var user = await EntityStore.ReconstituteAsync<User>(User.AddType(Context.GetEntityId()));
        if (user is null) return CommandResult.NotFound($"There is no user with username [{Context.GetEntityId()}]");

        // Perform operation(s) related to this command.
        try { availability.Claim(emailAddress); }
        catch (ArgumentOutOfRangeException exception) { return CommandResult.Forbidden(exception.Message); }
        user.SetEmailAddress(emailAddress);

        // Publish all the changes in a single atomic operation.
        await PublishChangesAsync(user, availability);
        return CommandResult.NoContent<string>();
    }
}
