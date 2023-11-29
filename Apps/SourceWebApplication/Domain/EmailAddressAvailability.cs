using Segerfeldt.EventStore.Source;

namespace SourceWebApplication.Domaim;

internal class EmailAddressAvailability : EntityBase
{
    public static readonly EntityType EntityType = new("EmailAddressAvailability");
    private static readonly EntityId SingletonEntityId = new("usernames");

    private const string EmailAddressClaimed = "EmailAddressClaimed";
    private const string EmailAddressReleased = "EmailAddressReleased";

    private readonly HashSet<string> usedEmailAddresses = new();

    public EmailAddressAvailability(EntityId id, EntityVersion version) : base(id, EntityType, version) { }

    internal static async Task<EmailAddressAvailability> GetAsync(IEntityStore entityStore)
    {
        var existingAvailability = await entityStore.ReconstituteAsync<EmailAddressAvailability>(SingletonEntityId, EntityType);
        return existingAvailability ?? new EmailAddressAvailability(SingletonEntityId, EntityVersion.New);
    }

    public void Claim(string emailAddress)
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
        usedEmailAddresses.Add(details.EmailAddress);
    }

    [ReplaysEvent(EmailAddressReleased)]
    public void OnEmailAddressReleased(EmailAddressDetails details)
    {
        usedEmailAddresses.Remove(details.EmailAddress);
    }

    internal record EmailAddressDetails(string EmailAddress);
}
