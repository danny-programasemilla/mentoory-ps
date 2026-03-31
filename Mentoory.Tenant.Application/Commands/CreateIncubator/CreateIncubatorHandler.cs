using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Tenant.Domain.Aggregates.Incubator;
using Mentoory.Tenant.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Mentoory.Tenant.Application.Commands.CreateIncubator;

/// <summary>
/// Handler for creating a new incubator.
/// </summary>
/// <param name="logger">Logger instance for tracking operations.</param>
/// <param name="repository">The repository for persisting incubator entities.</param>
/// <param name="timeProvider">The time provider for getting the current UTC time.</param>
public partial class CreateIncubatorHandler(
    ILogger<CreateIncubatorHandler> logger,
    IIncubatorRepository repository,
    ITimeProvider timeProvider)
    : BaseCommandHandler<CreateIncubatorCommand, Guid>
{
    /// <inheritdoc />
    public override Task<Result<Guid>> Handle(CreateIncubatorCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var incubator = Incubator.Create(request.Name, request.Description, timeProvider.UtcNow);
            repository.Add(incubator);

            LogIncubatorCreated(incubator.ExternalId);

            return Task.FromResult(Success(incubator.ExternalId));
        }
        catch (Exception ex)
        {
            LogIncubatorCreationFailed(request.Name, ex);
            return Task.FromResult(Failure(ResultErrorCodes.Unknown,
                (nameof(request.Name), "Error creating incubator")));
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Incubator created with ExternalId {ExternalId}")]
    partial void LogIncubatorCreated(Guid externalId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error creating incubator. {Name}")]
    partial void LogIncubatorCreationFailed(string name, Exception exception);
}
