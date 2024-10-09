using Segerfeldt.EventStore.Source;

namespace SourceWebApplication.Domaim;

internal sealed class User(EntityId id, EntityVersion version) : EntityBase(id, EntityType, version)
{
    public static readonly EntityType EntityType = new("User");

    internal static User New(EntityId entityId)
    {
        var user = new User(entityId, EntityVersion.New);
        user.Add(new UnpublishedEvent("Registered", new {}));
        return user;
    }

    internal static User New(string username, string emailAddress)
    {
        var user = new User(new EntityId(username), EntityVersion.New);
        user.SetEmailAddress(emailAddress);
        return user;
    }

    internal void SetEmailAddress(string emailAddress)
    {
        Add(new UnpublishedEvent("EmailAddressChanged", new { emailAddress }));
    }
}
