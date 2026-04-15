using Mentoory.Diagnostic.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Diagnostic.Application.Queries.GetEntrepreneurDiagnosticStatus;

public class GetEntrepreneurDiagnosticStatusHandler(
    IStageFormAssignmentRepository assignmentRepository,
    IDiagnosticResponseRepository responseRepository,
    IProjectFormRepository formRepository)
    : BaseCommandHandler<GetEntrepreneurDiagnosticStatusQuery, IReadOnlyList<StageFormStatusDto>>
{
    public override async Task<Result<IReadOnlyList<StageFormStatusDto>>> Handle(
        GetEntrepreneurDiagnosticStatusQuery request,
        CancellationToken cancellationToken)
    {
        var allAssignments = await assignmentRepository.ToListAsync(
            assignmentRepository.Query()
                .Where(a => a.ProjectId == request.ProjectId && a.IsActive),
            cancellationToken);

        var allResponses = await responseRepository.ToListAsync(
            responseRepository.Query()
                .Where(r => r.ProjectId == request.ProjectId
                          && r.EntrepreneurUserId == request.EntrepreneurUserId),
            cancellationToken);

        // Batch-load all referenced forms to avoid N+1
        var formIds = allAssignments.Select(a => a.ProjectFormId).Distinct().ToList();
        var forms = await formRepository.ToListAsync(
            formRepository.Query().Where(f => formIds.Contains(f.Id)),
            cancellationToken);
        var formMap = forms.ToDictionary(f => f.Id);

        var responseByAssignment = allResponses
            .ToDictionary(r => r.StageFormAssignmentId);

        var results = new List<StageFormStatusDto>();

        foreach (var assignment in allAssignments)
        {
            formMap.TryGetValue(assignment.ProjectFormId, out var form);
            var formName = form?.Name ?? "Formulario desconocido";

            responseByAssignment.TryGetValue(assignment.Id, out var response);

            results.Add(new StageFormStatusDto(
                assignment.ExternalId,
                assignment.ProjectStageId,
                formName,
                assignment.AssignedQuestions.Count,
                response?.IsCompleted ?? false,
                response?.CompletedAtUtc));
        }

        return Success((IReadOnlyList<StageFormStatusDto>)results);
    }
}
