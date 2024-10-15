using Segerfeldt.EventStore.Source;

namespace SourceWebApplication.Domaim;

internal sealed class EmailAddressAvailability(EntityId id, EntityVersion version) : EntityBase(id, EntityType, version)
{
    public static readonly EntityType EntityType = EntityType.Name("EmailAddressAvailability").OrThrow();
    private static readonly EntityId SingletonEntityId = EntityId.Value("usernames").OrThrow();

    private const string EmailAddressClaimed = "EmailAddressClaimed";
    private const string EmailAddressReleased = "EmailAddressReleased";

    private readonly HashSet<EmailAddress> usedEmailAddresses = [];

    internal static async Task<EmailAddressAvailability> GetAsync(EntityStore entityStore)
    {
        var existingAvailability = await entityStore.ReconstituteAsync<EmailAddressAvailability>(SingletonEntityId, EntityType);
        return existingAvailability ?? new EmailAddressAvailability(SingletonEntityId, EntityVersion.New);
    }

    public Result Claim(EmailAddress emailAddress)
    {
        if (usedEmailAddresses.Contains(emailAddress)) return Result.Failure($"The email address [{emailAddress}] is already claimed.");

        Add(new UnpublishedEvent(EmailAddressClaimed, new EmailAddressDetails(emailAddress)));
        return Result.Success;
    }

    public void Release(string emailAddress)
    {
        Add(new UnpublishedEvent(EmailAddressReleased, new EmailAddressDetails(emailAddress)));
    }

    [ReplaysEvent(EmailAddressClaimed)]
    public void OnEmailAddressClaimed(EmailAddressDetails details)
    {
        usedEmailAddresses.Add(EmailAddress.Of(details.EmailAddress).OrThrow());
    }

    [ReplaysEvent(EmailAddressReleased)]
    public void OnEmailAddressReleased(EmailAddressDetails details)
    {
        usedEmailAddresses.Remove(EmailAddress.Of(details.EmailAddress).OrThrow());
    }

    internal record EmailAddressDetails(string EmailAddress);
}

internal sealed class EmailAddress : ValueObject<EmailAddress>
{
    private readonly string value;

    private EmailAddress(string value) => this.value = value;

    public static Result<EmailAddress> Of(string value)
    {
        if (value == null) return Failure.Error($"invalid email address {value}");
        return new EmailAddress(value);
    }

    public static implicit operator string(EmailAddress value) => value.value;

    protected override IEnumerable<object> GetEqualityComponents() => [value];
}
