using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Diagnostic.Application.Queries.ListStageFormAssignments;

public sealed record ListStageFormAssignmentsQuery(long ProjectStageId) : IBaseRequest<IReadOnlyList<StageFormAssignmentSummaryDto>>;
