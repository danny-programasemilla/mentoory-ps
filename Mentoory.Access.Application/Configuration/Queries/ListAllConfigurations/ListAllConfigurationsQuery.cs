using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Configuration.Queries.ListAllConfigurations;

/// <summary>
/// Query to retrieve all system configuration entries.
/// </summary>
public sealed record ListAllConfigurationsQuery : IBaseRequest<List<ConfigurationListItemDto>>;
