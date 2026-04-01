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
    private readonly IProjectFormRepository _projectFormRepository;
    private readonly IDiagnosticResponseRepository _diagnosticResponseRepository;
    private readonly ITimeProvider _timeProvider;
    private readonly IIntegrationEventService _eventService;
    private readonly ILogger<SubmitDiagnosticResponseHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubmitDiagnosticResponseHandler"/> class.
    /// </summary>
    /// <param name="projectFormRepository">The project form repository to validate the form exists.</param>
    /// <param name="diagnosticResponseRepository">The diagnostic response repository for persistence operations.</param>
    /// <param name="timeProvider">The time provider for obtaining current UTC time.</param>
    /// <param name="eventService">The integration event service for publishing events.</param>
    /// <param name="logger">The logger instance.</param>
    public SubmitDiagnosticResponseHandler(
        IProjectFormRepository projectFormRepository,
        IDiagnosticResponseRepository diagnosticResponseRepository,
        ITimeProvider timeProvider,
        IIntegrationEventService eventService,
        ILogger<SubmitDiagnosticResponseHandler> logger)
    {
        _projectFormRepository = projectFormRepository;
        _diagnosticResponseRepository = diagnosticResponseRepository;
        _timeProvider = timeProvider;
        _eventService = eventService;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<Result> Handle(SubmitDiagnosticResponseCommand request, CancellationToken cancellationToken)
    {
        var projectForm = await _projectFormRepository.GetByExternalIdAsync(
            request.ProjectFormExternalId, request.ProjectId, cancellationToken);

        if (projectForm is null)
        {
            return Failure(ResultErrorCodes.GenericError,
                ("ProjectForm", "El formulario del proyecto no fue encontrado o no pertenece al proyecto activo."));
        }

        var utcNow = _timeProvider.UtcNow;

        var diagnosticResponse = Domain.Aggregates.DiagnosticResponse.DiagnosticResponse.Create(
            projectForm.Id,
            request.ProjectId,
            request.IncubatorId,
            request.EntrepreneurUserId,
            request.EvaluationStage,
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

        LogDiagnosticSubmitted(diagnosticResponse.ExternalId, projectForm.ExternalId);

        await _eventService.PublishAsync(
            new DiagnosticCompletedEvent(
                diagnosticResponse.Id,
                projectForm.Id,
                request.ProjectId,
                request.IncubatorId,
                request.EntrepreneurUserId,
                request.EvaluationStage,
                utcNow),
            cancellationToken);

        return Success();
    }

    /// <summary>
    /// Logs when a diagnostic response is successfully submitted.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Diagnostic response {DiagnosticExternalId} submitted for project form {ProjectFormExternalId}")]
    partial void LogDiagnosticSubmitted(Guid diagnosticExternalId, Guid projectFormExternalId);
}
