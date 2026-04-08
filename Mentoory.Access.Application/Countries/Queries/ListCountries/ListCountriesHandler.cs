using Mentoory.Access.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Countries.Queries.ListCountries;

public class ListCountriesHandler(ICountryRepository countryRepository)
    : BaseCommandHandler<ListCountriesQuery, List<CountryDto>>
{
    public override async Task<Result<List<CountryDto>>> Handle(
        ListCountriesQuery request,
        CancellationToken cancellationToken)
    {
        var countries = await countryRepository.GetAllActiveAsync(cancellationToken);

        var dtos = countries
            .Select(c => new CountryDto(
                c.ExternalId,
                c.Name,
                c.Code,
                c.IdentificationLabel,
                c.IdentificationMask,
                c.IdentificationRegex,
                c.IdentificationMaxLength))
            .ToList();

        return Success(dtos);
    }
}
