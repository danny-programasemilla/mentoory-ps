using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Tenant.Application.Queries.ListIncubators;
using Mentoory.Tenant.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Mentoory.Tenant.Application.Queries.GetIncubatorByExternalId;

/// <summary>
/// Handler for retrieving a single incubator by its external identifier.
/// </summary>
/// <param name="logger">Logger instance for tracking operations.</param>
/// <param name="repository">The repository for accessing incubator entities.</param>
public partial class GetIncubatorByExternalIdHandler(
    ILogger<GetIncubatorByExternalIdHandler> logger,
    IIncubatorRepository repository)
    : BaseCommandHandler<GetIncubatorByExternalIdQuery, IncubatorDto>
{
    /// <inheritdoc />
    public override async Task<Result<IncubatorDto>> Handle(
        GetIncubatorByExternalIdQuery request,
        CancellationToken cancellationToken)
    {
        var incubator = await repository.GetByExternalIdAsync(request.ExternalId, cancellationToken);

        if (incubator is null)
        {
            LogIncubatorNotFound(request.ExternalId);
            return Failure(ResultErrorCodes.GenericError,
                (nameof(request.ExternalId), "Incubator not found"));
        }

        var dto = new IncubatorDto(
            incubator.ExternalId,
            incubator.Name,
            incubator.Description,
            incubator.IsActive,
            incubator.CreatedAtUtc,
            incubator.UpdatedAtUtc);

        return Success(dto);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Incubator not found with ExternalId {ExternalId}")]
    partial void LogIncubatorNotFound(Guid externalId);
}
