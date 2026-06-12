using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Tenant.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Mentoory.Tenant.Application.Commands.UpdateIncubator;

/// <summary>
/// Handler for updating an existing incubator.
/// </summary>
/// <param name="logger">Logger instance for tracking operations.</param>
/// <param name="repository">The repository for persisting incubator entities.</param>
/// <param name="timeProvider">The time provider for getting the current UTC time.</param>
public partial class UpdateIncubatorHandler(
    ILogger<UpdateIncubatorHandler> logger,
    IIncubatorRepository repository,
    ITimeProvider timeProvider)
    : BaseCommandHandler<UpdateIncubatorCommand>
{
    /// <inheritdoc />
    public override async Task<Result> Handle(UpdateIncubatorCommand request, CancellationToken cancellationToken)
    {
        var incubator = await repository.GetByExternalIdAsync(request.ExternalId, cancellationToken);

        if (incubator is null)
        {
            LogIncubatorNotFound(request.ExternalId);
            return Failure(ResultErrorCodes.GenericError,
                (nameof(request.ExternalId), "Incubator not found"));
        }

        incubator.Update(request.Name, request.Description, timeProvider.UtcNow);

        if (request.IsActive)
        {
            incubator.Activate(timeProvider.UtcNow);
        }
        else
        {
            incubator.Deactivate(timeProvider.UtcNow);
        }

        repository.Update(incubator);

        LogIncubatorUpdated(request.ExternalId);

        return Success();
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Incubator not found with ExternalId {ExternalId}")]
    partial void LogIncubatorNotFound(Guid externalId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Incubator updated with ExternalId {ExternalId}")]
    partial void LogIncubatorUpdated(Guid externalId);
}
