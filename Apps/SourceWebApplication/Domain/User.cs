using Segerfeldt.EventStore.Source;

namespace SourceWebApplication.Domaim;

internal sealed class User(EntityId id, EntityVersion version) : EntityBase(id, EntityType, version)
{
    public static readonly EntityType EntityType = EntityType.Name("User").OrThrow();

    internal static User New(EntityId entityId)
    {
        var user = new User(entityId, EntityVersion.New);
        user.Add(new UnpublishedEvent("Registered", new {}));
        return user;
    }

    internal static Result<User> New(string username, string emailAddress)
    {
        var entityId = EntityId.Value(username);

        return entityId.IfSuccess<User>(entityId => {
            var user = new User(entityId, EntityVersion.New);
            user.SetEmailAddress(emailAddress);
            return user;
        });
    }

    internal void SetEmailAddress(string emailAddress)
    {
        Add(new UnpublishedEvent("EmailAddressChanged", new { emailAddress }));
    }
}
