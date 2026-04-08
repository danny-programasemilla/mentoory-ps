using Mentoory.Access.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Configuration.Queries.GetConfiguration;

/// <summary>
/// Handles the query to retrieve a system configuration value by key.
/// </summary>
public class GetConfigurationHandler : BaseCommandHandler<GetConfigurationQuery, string>
{
    private readonly ISystemConfigurationRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetConfigurationHandler"/> class.
    /// </summary>
    /// <param name="repository">The system configuration repository for data access.</param>
    public GetConfigurationHandler(ISystemConfigurationRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc />
    public override async Task<Result<string>> Handle(GetConfigurationQuery request, CancellationToken cancellationToken)
    {
        var config = await _repository.GetByKeyAsync(request.Key, cancellationToken);

        if (config is null)
        {
            return Failure(ResultErrorCodes.GenericError, ("GetConfiguration", $"Clave de configuración '{request.Key}' no encontrada."));
        }

        return Success(config.Value);
    }
}
