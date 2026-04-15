using Mentoory.Diagnostic.Application.IntegrationEvents;
using Mentoory.Diagnostic.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.IntegrationEvents;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Microsoft.Extensions.Logging;

namespace Mentoory.Diagnostic.Application.Commands.SubmitDiagnosticResponse;

/// <summary>
/// Handles the submission of a diagnostic response, creating responses for each question and marking the diagnostic as complete.
/// </summary>
public partial class SubmitDiagnosticResponseHandler : BaseCommandHandler<SubmitDiagnosticResponseCommand>
{
    private readonly IStageFormAssignmentRepository _stageFormAssignmentRepository;
    private readonly IDiagnosticResponseRepository _diagnosticResponseRepository;
    private readonly ITimeProvider _timeProvider;
    private readonly IIntegrationEventService _eventService;
    private readonly ILogger<SubmitDiagnosticResponseHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubmitDiagnosticResponseHandler"/> class.
    /// </summary>
    public SubmitDiagnosticResponseHandler(
        IStageFormAssignmentRepository stageFormAssignmentRepository,
        IDiagnosticResponseRepository diagnosticResponseRepository,
        ITimeProvider timeProvider,
        IIntegrationEventService eventService,
        ILogger<SubmitDiagnosticResponseHandler> logger)
    {
        _stageFormAssignmentRepository = stageFormAssignmentRepository;
        _diagnosticResponseRepository = diagnosticResponseRepository;
        _timeProvider = timeProvider;
        _eventService = eventService;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<Result> Handle(SubmitDiagnosticResponseCommand request, CancellationToken cancellationToken)
    {
        var assignment = await _stageFormAssignmentRepository.GetByExternalIdAsync(
            request.StageFormAssignmentExternalId, cancellationToken);

        if (assignment is null || !assignment.IsActive)
        {
            return Failure(
                ResultErrorCodes.GenericError,
                ("StageFormAssignment", "La asignación de formulario no fue encontrada o está inactiva."));
        }

        var utcNow = _timeProvider.UtcNow;

        var diagnosticResponse = Domain.Aggregates.DiagnosticResponse.DiagnosticResponse.Create(
            assignment.ProjectFormId,
            request.ProjectId,
            request.IncubatorId,
            request.EntrepreneurUserId,
            assignment.Id,
            utcNow);

        foreach (var item in request.Responses)
        {
            diagnosticResponse.AddResponse(
                item.QuestionId,
                item.TextValue,
                item.NumericValue,
                item.SelectedOptionIds,
                utcNow);
        }

        diagnosticResponse.MarkAsCompleted(utcNow);

        _diagnosticResponseRepository.Add(diagnosticResponse);
        await _diagnosticResponseRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        LogDiagnosticSubmitted(diagnosticResponse.ExternalId, assignment.ExternalId);

        await _eventService.PublishAsync(
            new DiagnosticCompletedEvent(
                diagnosticResponse.Id,
                assignment.ProjectFormId,
                request.ProjectId,
                request.IncubatorId,
                request.EntrepreneurUserId,
                assignment.Id,
                assignment.ProjectStageId,
                utcNow),
            cancellationToken);

        return Success();
    }

    /// <summary>
    /// Logs when a diagnostic response is successfully submitted.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Diagnostic response {DiagnosticExternalId} submitted for assignment {AssignmentExternalId}")]
    partial void LogDiagnosticSubmitted(Guid diagnosticExternalId, Guid assignmentExternalId);
}
