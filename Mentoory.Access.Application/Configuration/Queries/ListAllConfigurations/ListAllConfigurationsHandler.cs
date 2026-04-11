using Mentoory.Access.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Configuration.Queries.ListAllConfigurations;

/// <summary>
/// Handles the query to retrieve all system configuration entries.
/// </summary>
public class ListAllConfigurationsHandler : BaseCommandHandler<ListAllConfigurationsQuery, List<ConfigurationListItemDto>>
{
    private readonly ISystemConfigurationRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListAllConfigurationsHandler"/> class.
    /// </summary>
    /// <param name="repository">The system configuration repository for data access.</param>
    public ListAllConfigurationsHandler(ISystemConfigurationRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc />
    public override async Task<Result<List<ConfigurationListItemDto>>> Handle(ListAllConfigurationsQuery request, CancellationToken cancellationToken)
    {
        var configs = await _repository.GetAllAsync(cancellationToken);

        var items = configs.Select(c => new ConfigurationListItemDto(
            c.Key,
            c.Value,
            c.Description,
            c.DataType,
            c.UpdatedAtUtc)).ToList();

        return Success(items);
    }
}
