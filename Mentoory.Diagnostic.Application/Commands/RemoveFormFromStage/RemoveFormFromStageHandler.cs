using Mentoory.Diagnostic.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Microsoft.Extensions.Logging;

namespace Mentoory.Diagnostic.Application.Commands.RemoveFormFromStage;

public partial class RemoveFormFromStageHandler(
    ILogger<RemoveFormFromStageHandler> logger,
    IStageFormAssignmentRepository assignmentRepository)
    : BaseCommandHandler<RemoveFormFromStageCommand>
{
    public override async Task<Result> Handle(RemoveFormFromStageCommand request, CancellationToken cancellationToken)
    {
        var assignment = await assignmentRepository.GetByExternalIdAsync(request.AssignmentExternalId, cancellationToken);
        if (assignment is null)
        {
            LogAssignmentNotFound(request.AssignmentExternalId);
            return Failure(ResultErrorCodes.GenericError,
                (nameof(request.AssignmentExternalId), "Asignación no encontrada."));
        }

        assignment.Deactivate();
        LogAssignmentDeactivated(request.AssignmentExternalId);
        return Success();
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Stage form assignment not found with ExternalId {ExternalId}")]
    partial void LogAssignmentNotFound(Guid externalId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stage form assignment {ExternalId} deactivated")]
    partial void LogAssignmentDeactivated(Guid externalId);
}
