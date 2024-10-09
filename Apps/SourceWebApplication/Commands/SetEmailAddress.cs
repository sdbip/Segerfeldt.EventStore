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
        var emailAddress = command.EmailAddress;
        var availability = await EmailAddressAvailability.GetAsync(context.EntityStore);

        try
        {
            availability.Claim(emailAddress);
        }
        catch (Exception exception)
        {
            return CommandResult.Forbidden(exception.Message);
        }

        var id = new EntityId(context.GetRouteParameter("entityid"));
        var entity = await context.EntityStore.ReconstituteAsync<User>(id, User.EntityType);
        if (entity is null) return CommandResult.NotFound($"There is no user with username [{id}]");

        entity.SetEmailAddress(emailAddress);

        await context.EventPublisher.PublishChangesAsync(entity, "test_user");
        await context.EventPublisher.PublishChangesAsync(availability, "test_user");
        return CommandResult.NoContent();
    }
}
