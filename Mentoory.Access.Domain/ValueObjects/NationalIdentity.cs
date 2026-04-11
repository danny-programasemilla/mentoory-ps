using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Access.Domain.ValueObjects;

public class NationalIdentity : ValueObject
{
    public NationalIdentity(string country, string nationalId)
    {
        if (string.IsNullOrWhiteSpace(country))
        {
            throw new ArgumentException("Country is required.", nameof(country));
        }

        if (string.IsNullOrWhiteSpace(nationalId))
        {
            throw new ArgumentException("National ID is required.", nameof(nationalId));
        }

        Country = country.Trim();
        NationalId = nationalId.Trim();
    }

    private NationalIdentity()
    {
        Country = null!;
        NationalId = null!;
    }

    public string Country { get; private set; }
    public string NationalId { get; private set; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Country;
        yield return NationalId;
    }
}
