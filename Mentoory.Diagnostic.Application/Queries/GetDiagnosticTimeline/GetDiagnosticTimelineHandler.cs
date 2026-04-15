using Mentoory.Diagnostic.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Diagnostic.Application.Queries.GetDiagnosticTimeline;

public class GetDiagnosticTimelineHandler(
    IDiagnosticResponseRepository responseRepository,
    IStageFormAssignmentRepository assignmentRepository,
    IProjectFormRepository formRepository)
    : BaseCommandHandler<GetDiagnosticTimelineQuery, IReadOnlyList<DiagnosticTimelineEntryDto>>
{
    public override async Task<Result<IReadOnlyList<DiagnosticTimelineEntryDto>>> Handle(
        GetDiagnosticTimelineQuery request,
        CancellationToken cancellationToken)
    {
        var responses = await responseRepository.ToListAsync(
            responseRepository.Query()
                .Where(r => r.ProjectId == request.ProjectId
                          && r.EntrepreneurUserId == request.EntrepreneurUserId
                          && r.IsCompleted),
            cancellationToken);

        // Batch-load all referenced assignments and forms to avoid N+1
        var assignmentIds = responses.Select(r => r.StageFormAssignmentId).Distinct().ToList();
        var formIds = responses.Select(r => r.ProjectFormId).Distinct().ToList();

        var assignments = await assignmentRepository.ToListAsync(
            assignmentRepository.Query().Where(a => assignmentIds.Contains(a.Id)),
            cancellationToken);
        var forms = await formRepository.ToListAsync(
            formRepository.Query().Where(f => formIds.Contains(f.Id)),
            cancellationToken);

        var assignmentMap = assignments.ToDictionary(a => a.Id);
        var formMap = forms.ToDictionary(f => f.Id);

        var results = responses
            .OrderBy(r => r.CompletedAtUtc)
            .Select(response =>
            {
                assignmentMap.TryGetValue(response.StageFormAssignmentId, out var assignment);
                formMap.TryGetValue(response.ProjectFormId, out var form);

                return new DiagnosticTimelineEntryDto(
                    response.ExternalId,
                    assignment?.ExternalId ?? Guid.Empty,
                    assignment?.ProjectStageId ?? 0,
                    form?.Name ?? "Formulario desconocido",
                    response.CompletedAtUtc!.Value,
                    response.QuestionResponses.Count);
            })
            .ToList();

        return Success((IReadOnlyList<DiagnosticTimelineEntryDto>)results);
    }
}
