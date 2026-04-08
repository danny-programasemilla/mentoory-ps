namespace Mentoory.Access.Application.Countries.Queries.ListCountries;

public sealed record CountryDto(
    Guid ExternalId,
    string Name,
    string Code,
    string IdentificationLabel,
    string? IdentificationMask,
    string? IdentificationRegex,
    int IdentificationMaxLength);
