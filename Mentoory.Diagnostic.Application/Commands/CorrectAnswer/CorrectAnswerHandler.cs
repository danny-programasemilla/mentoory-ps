using Mentoory.Diagnostic.Application.IntegrationEvents;
using Mentoory.Diagnostic.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.IntegrationEvents;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Microsoft.Extensions.Logging;

namespace Mentoory.Diagnostic.Application.Commands.CorrectAnswer;

/// <summary>
/// Handles the correction of an answer in a completed diagnostic response.
/// </summary>
public partial class CorrectAnswerHandler : BaseCommandHandler<CorrectAnswerCommand>
{
    private readonly IDiagnosticResponseRepository _diagnosticResponseRepository;
    private readonly ITimeProvider _timeProvider;
    private readonly IIntegrationEventService _eventService;
    private readonly ILogger<CorrectAnswerHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CorrectAnswerHandler"/> class.
    /// </summary>
    /// <param name="diagnosticResponseRepository">The diagnostic response repository for persistence operations.</param>
    /// <param name="timeProvider">The time provider for obtaining current UTC time.</param>
    /// <param name="eventService">The integration event service for publishing events.</param>
    /// <param name="logger">The logger instance.</param>
    public CorrectAnswerHandler(
        IDiagnosticResponseRepository diagnosticResponseRepository,
        ITimeProvider timeProvider,
        IIntegrationEventService eventService,
        ILogger<CorrectAnswerHandler> logger)
    {
        _diagnosticResponseRepository = diagnosticResponseRepository;
        _timeProvider = timeProvider;
        _eventService = eventService;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<Result> Handle(CorrectAnswerCommand request, CancellationToken cancellationToken)
    {
        var diagnosticResponse = request.ProjectId.HasValue
            ? await _diagnosticResponseRepository.GetByExternalIdWithResponsesAsync(
                request.DiagnosticResponseExternalId, request.ProjectId.Value, cancellationToken)
            : await _diagnosticResponseRepository.GetByExternalIdWithResponsesAsync(
                request.DiagnosticResponseExternalId, cancellationToken);

        if (diagnosticResponse is null)
        {
            return Failure(ResultErrorCodes.GenericError,
                ("DiagnosticResponse", "La respuesta diagnóstica no fue encontrada o no pertenece al proyecto activo."));
        }

        var utcNow = _timeProvider.UtcNow;

        diagnosticResponse.CorrectAnswer(
            request.QuestionResponseId,
            request.NewTextValue,
            request.NewNumericValue,
            request.NewSelectedOptionIds,
            request.CorrectedByUserId,
            request.Reason,
            utcNow);

        _diagnosticResponseRepository.Update(diagnosticResponse);
        await _diagnosticResponseRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        LogAnswerCorrected(diagnosticResponse.ExternalId, request.QuestionResponseId);

        await _eventService.PublishAsync(
            new AnswerCorrectedEvent(
                diagnosticResponse.Id,
                request.QuestionResponseId,
                request.CorrectedByUserId,
                utcNow),
            cancellationToken);

        return Success();
    }

    /// <summary>
    /// Logs when an answer is successfully corrected.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Answer corrected in diagnostic response {DiagnosticExternalId} for question response {QuestionResponseId}")]
    partial void LogAnswerCorrected(Guid diagnosticExternalId, long questionResponseId);
}
