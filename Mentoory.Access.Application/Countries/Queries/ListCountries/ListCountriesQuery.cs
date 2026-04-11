using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Countries.Queries.ListCountries;

public sealed record ListCountriesQuery() : IBaseRequest<List<CountryDto>>;
