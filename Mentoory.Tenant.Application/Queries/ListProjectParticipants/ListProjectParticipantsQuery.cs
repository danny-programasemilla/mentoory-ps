using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Tenant.Application.Queries.ListProjectParticipants;

public sealed record ListProjectParticipantsQuery(Guid ProjectExternalId) : IBaseRequest<List<ProjectParticipantDto>>;
