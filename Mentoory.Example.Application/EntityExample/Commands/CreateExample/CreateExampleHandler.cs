using Mentoory.Example.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Microsoft.Extensions.Logging;

namespace Mentoory.Example.Application.EntityExample.Commands.CreateExample;

/// <summary>
/// Handler for the CreateExampleCommand that creates a new example entity.
/// </summary>
/// <param name="logger">Logger instance for tracking operations.</param>
/// <param name="repository">The repository for persisting example entities.</param>
public partial class CreateExampleHandler(ILogger<CreateExampleHandler> logger, IExampleRepository repository)
    : BaseCommandHandler<CreateExampleCommand, Domain.Aggregates.Example.Example>
{
    /// <summary>
    /// Handles the command to create a new example entity.
    /// </summary>
    /// <param name="request">The command containing the example data to create.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the created example entity or a failure result.</returns>
    public override Task<Result<Domain.Aggregates.Example.Example>> Handle(CreateExampleCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var entity = repository.Add(new Domain.Aggregates.Example.Example(request.Title, request.DateCreated));
            return Task.FromResult(Success(entity));
        }
        catch (Exception ex)
        {
            LogCreatingExampleFailed(request.Title, ex);
            return Task.FromResult(Failure(ResultErrorCodes.Unknown,
                (nameof(request.Title), "Error by adding new example")));
        }
    }

    /// <summary>
    /// Logs when creating an example fails.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Error creating example. {Title}")]
    partial void LogCreatingExampleFailed(string title, Exception exception);
}
