using Mentoory.Diagnostic.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Diagnostic.Application.Queries.ListStageFormAssignments;

public class ListStageFormAssignmentsHandler(
    IStageFormAssignmentRepository assignmentRepository,
    IProjectFormRepository projectFormRepository)
    : BaseCommandHandler<ListStageFormAssignmentsQuery, IReadOnlyList<StageFormAssignmentSummaryDto>>
{
    public override async Task<Result<IReadOnlyList<StageFormAssignmentSummaryDto>>> Handle(
        ListStageFormAssignmentsQuery request,
        CancellationToken cancellationToken)
    {
        var assignments = await assignmentRepository.GetByProjectStageIdAsync(request.ProjectStageId, cancellationToken);

        var activeAssignments = assignments.Where(a => a.IsActive).ToList();

        // Batch-load all referenced forms to avoid N+1
        var formIds = activeAssignments.Select(a => a.ProjectFormId).Distinct().ToList();
        var forms = await projectFormRepository.ToListAsync(
            projectFormRepository.Query().Where(f => formIds.Contains(f.Id)),
            cancellationToken);
        var formMap = forms.ToDictionary(f => f.Id);

        var results = activeAssignments.Select(assignment =>
        {
            formMap.TryGetValue(assignment.ProjectFormId, out var form);
            return new StageFormAssignmentSummaryDto(
                assignment.ExternalId,
                form?.Name ?? "Formulario desconocido",
                assignment.AssignedQuestions.Count,
                assignment.IsActive,
                assignment.CreatedAtUtc);
        }).ToList();

        return Success((IReadOnlyList<StageFormAssignmentSummaryDto>)results);
    }
}
