using Segerfeldt.EventStore.Source;

namespace RefactoringWebApplication.Domain;

internal sealed class User(EntityId id, EntityVersion version) : EntityBase(id, EntityType, version)
{
    public static readonly EntityType EntityType = EntityType.Name("User");

    internal static User New(EntityId entityId, EmailAddress? emailAddress = null)
    {
        var user = new User(entityId, EntityVersion.New);
        user.Add(new UnpublishedEvent("Registered", new { }));
        if (emailAddress is not null) user.SetEmailAddress(emailAddress);
        return user;
    }

    internal void SetEmailAddress(EmailAddress emailAddress)
    {
        Add(new UnpublishedEvent("EmailAddressChanged", new { emailAddress }));
    }
}
