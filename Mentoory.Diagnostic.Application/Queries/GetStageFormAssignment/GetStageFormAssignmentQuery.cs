using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Diagnostic.Application.Queries.GetStageFormAssignment;

public sealed record GetStageFormAssignmentQuery(Guid AssignmentExternalId) : IBaseRequest<StageFormAssignmentDto>;
