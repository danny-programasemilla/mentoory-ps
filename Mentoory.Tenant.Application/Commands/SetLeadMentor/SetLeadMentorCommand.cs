using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Tenant.Application.Commands.SetLeadMentor;

public sealed record SetLeadMentorCommand(Guid ProjectExternalId, Guid MentorAssignmentExternalId) : IBaseRequest;
