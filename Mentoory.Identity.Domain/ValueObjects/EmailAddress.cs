using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Identity.Domain.ValueObjects;

public class EmailAddress : ValueObject
{
    public EmailAddress(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email address cannot be empty.", nameof(email));
        }

        Value = email.Trim();
        NormalizedValue = Value.ToUpperInvariant();
    }

    private EmailAddress()
    {
        Value = null!;
        NormalizedValue = null!;
    }

    public string Value { get; private set; }
    public string NormalizedValue { get; private set; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return NormalizedValue;
    }
}
