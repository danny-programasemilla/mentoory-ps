using Mentoory.Diagnostic.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Diagnostic.Application.Queries.GetStageFormNames;

public sealed class GetStageFormNamesHandler(
    IStageFormAssignmentRepository assignmentRepository,
    IProjectFormRepository projectFormRepository)
    : BaseCommandHandler<GetStageFormNamesQuery, StageFormNamesDto>
{
    public override async Task<Result<StageFormNamesDto>> Handle(
        GetStageFormNamesQuery request,
        CancellationToken cancellationToken)
    {
        var assignments = await assignmentRepository.ToListAsync(
            assignmentRepository.Query()
                .Where(a => a.ProjectId == request.ProjectId && a.IsActive)
                .Select(a => new { a.ProjectStageId, a.ProjectFormId }),
            cancellationToken);

        if (assignments.Count == 0)
        {
            return Success(new StageFormNamesDto(
                new Dictionary<long, IReadOnlyList<string>>()));
        }

        var formIds = assignments.Select(a => a.ProjectFormId).Distinct().ToList();
        var formNames = await projectFormRepository.ToListAsync(
            projectFormRepository.Query()
                .Where(f => formIds.Contains(f.Id))
                .Select(f => new { f.Id, f.Name }),
            cancellationToken);
        var formMap = formNames.ToDictionary(f => f.Id, f => f.Name);

        var grouped = assignments
            .Where(a => formMap.ContainsKey(a.ProjectFormId))
            .GroupBy(a => a.ProjectStageId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<string>)g
                    .Select(a => formMap[a.ProjectFormId])
                    .Distinct()
                    .OrderBy(name => name)
                    .ToList());

        return Success(new StageFormNamesDto(grouped));
    }
}
