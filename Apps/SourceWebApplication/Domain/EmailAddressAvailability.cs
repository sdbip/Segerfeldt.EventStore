using Segerfeldt.EventStore.Source;

namespace SourceWebApplication.Domain;

internal sealed class EmailAddressAvailability(EntityId id, EntityVersion version) : EntityBase(id, EntityType, version)
{
    public static readonly EntityType EntityType = EntityType.Name("EmailAddressAvailability");
    private static readonly EntityId SingletonEntityId = EntityId.Value("usernames");

    private const string EmailAddressClaimed = "EmailAddressClaimed";
    private const string EmailAddressReleased = "EmailAddressReleased";

    private readonly HashSet<EmailAddress> usedEmailAddresses = [];

    internal static async Task<EmailAddressAvailability> GetAsync(EntityStore entityStore)
    {
        var existingAvailability = await entityStore.ReconstituteAsync<EmailAddressAvailability>(SingletonEntityId, EntityType);
        return existingAvailability ?? new EmailAddressAvailability(SingletonEntityId, EntityVersion.New);
    }

    public void Claim(EmailAddress emailAddress)
    {
        if (usedEmailAddresses.Contains(emailAddress)) throw new Exception($"The email address [{emailAddress}] is already claimed.");

        Add(new UnpublishedEvent(EmailAddressClaimed, new EmailAddressDetails(emailAddress)));
    }

    public void Release(string emailAddress)
    {
        Add(new UnpublishedEvent(EmailAddressReleased, new EmailAddressDetails(emailAddress)));
    }

    [ReplaysEvent(EmailAddressClaimed)]
    public void OnEmailAddressClaimed(EmailAddressDetails details)
    {
        usedEmailAddresses.Add(EmailAddress.Prevalidated(details.EmailAddress));
    }

    [ReplaysEvent(EmailAddressReleased)]
    public void OnEmailAddressReleased(EmailAddressDetails details)
    {
        usedEmailAddresses.Remove(EmailAddress.Prevalidated(details.EmailAddress));
    }

    internal record EmailAddressDetails(string EmailAddress);
}
