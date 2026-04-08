using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Access.Domain.Aggregates.Country;

public class Country : Entity, IAggregateRoot
{
    private Country()
    {
    }

    public Guid ExternalId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Code { get; private set; } = null!;
    public string IdentificationLabel { get; private set; } = null!;
    public string? IdentificationMask { get; private set; }
    public string? IdentificationRegex { get; private set; }
    public int IdentificationMaxLength { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static Country Create(
        string name,
        string code,
        string identificationLabel,
        string? identificationMask,
        string? identificationRegex,
        int identificationMaxLength,
        DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Country name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Country code is required.", nameof(code));
        }

        if (identificationMaxLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(identificationMaxLength), "Must be greater than zero.");
        }

        return new Country
        {
            ExternalId = Guid.NewGuid(),
            Name = name.Trim(),
            Code = code.Trim().ToUpperInvariant(),
            IdentificationLabel = identificationLabel,
            IdentificationMask = identificationMask,
            IdentificationRegex = identificationRegex,
            IdentificationMaxLength = identificationMaxLength,
            IsActive = true,
            CreatedAtUtc = utcNow,
        };
    }
}
