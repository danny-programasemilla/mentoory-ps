using System.Text.Json;
using Mentoory.Diagnostic.Application.IntegrationEvents;
using Mentoory.Diagnostic.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.Audit;
using Mentoory.Shared.Application.IntegrationEvents;
using Mentoory.Shared.Application.Interfaces;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Microsoft.Extensions.Logging;

namespace Mentoory.Diagnostic.Application.Commands.CorrectAnswer;

/// <summary>
/// Handles the correction of an answer in a completed diagnostic response.
/// Writes the audit entry manually (Manual-mode) so the row captures BOTH the previous
/// answer value and the new one, which the pipeline behavior cannot observe from the
/// request payload alone.
/// </summary>
public partial class CorrectAnswerHandler : BaseCommandHandler<CorrectAnswerCommand>
{
    private readonly IDiagnosticResponseRepository _diagnosticResponseRepository;
    private readonly ITimeProvider _timeProvider;
    private readonly IIntegrationEventService _eventService;
    private readonly IAuditService _auditService;
    private readonly ITenantContext _tenantContext;
    private readonly ICorrelationContext _correlationContext;
    private readonly ILogger<CorrectAnswerHandler> _logger;

    public CorrectAnswerHandler(
        IDiagnosticResponseRepository diagnosticResponseRepository,
        ITimeProvider timeProvider,
        IIntegrationEventService eventService,
        IAuditService auditService,
        ITenantContext tenantContext,
        ICorrelationContext correlationContext,
        ILogger<CorrectAnswerHandler> logger)
    {
        _diagnosticResponseRepository = diagnosticResponseRepository;
        _timeProvider = timeProvider;
        _eventService = eventService;
        _auditService = auditService;
        _tenantContext = tenantContext;
        _correlationContext = correlationContext;
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

        var previousAnswer = diagnosticResponse.QuestionResponses
            .FirstOrDefault(r => r.Id == request.QuestionResponseId);
        var beforeSnapshot = previousAnswer is null
            ? null
            : new
            {
                previousAnswer.TextValue,
                previousAnswer.NumericValue,
                SelectedOptionIds = previousAnswer.SelectedOptionIds.ToArray(),
            };

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

        await WriteAuditAsync(request, diagnosticResponse.ExternalId, beforeSnapshot, utcNow, cancellationToken);

        return Success();
    }

    private Task WriteAuditAsync(
        CorrectAnswerCommand request,
        Guid diagnosticExternalId,
        object? beforeSnapshot,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var details = JsonSerializer.Serialize(new
        {
            Before = beforeSnapshot,
            After = new
            {
                request.NewTextValue,
                request.NewNumericValue,
                NewSelectedOptionIds = request.NewSelectedOptionIds?.ToArray(),
            },
            request.Reason,
        });

        var entry = new AuditEntry(
            EventType: AuditEventTypes.AnswerCorrected,
            UserId: _tenantContext.UserId ?? request.CorrectedByUserId,
            IncubatorId: _tenantContext.IncubatorId,
            ProjectId: _tenantContext.ProjectId ?? request.ProjectId,
            EntityType: "AnswerCorrection",
            EntityId: diagnosticExternalId.ToString(),
            Action: nameof(CorrectAnswerCommand),
            Details: details,
            IpAddress: _correlationContext.ClientIpAddress,
            OccurredAtUtc: utcNow,
            CorrelationId: _correlationContext.CorrelationId,
            Outcome: "Success",
            ExceptionType: null,
            UserEmail: _tenantContext.UserEmail,
            RoleContext: _tenantContext.Role);

        return _auditService.LogAsync(entry, cancellationToken);
    }

    /// <summary>
    /// Logs when an answer is successfully corrected.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Answer corrected in diagnostic response {DiagnosticExternalId} for question response {QuestionResponseId}")]
    partial void LogAnswerCorrected(Guid diagnosticExternalId, long questionResponseId);
}
