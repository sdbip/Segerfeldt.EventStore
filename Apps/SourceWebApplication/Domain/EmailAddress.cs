using Segerfeldt.EventStore.Source;

namespace SourceWebApplication.Domain;

internal sealed class EmailAddress : ValueObject<EmailAddress>
{
    private readonly string value;

    private EmailAddress(string value) => this.value = value;

    public static EmailAddress Of(string value)
    {
        ArgumentException.ThrowIfNullOrEmpty($"invalid email address {value}", nameof(value));
        return new EmailAddress(value);
    }

    public override string ToString() => value;

    public static EmailAddress Prevalidated(string value) => new(value);

    public static implicit operator string(EmailAddress value) => value.value;

    protected override IEnumerable<object> GetEqualityComponents() => [value];
}
