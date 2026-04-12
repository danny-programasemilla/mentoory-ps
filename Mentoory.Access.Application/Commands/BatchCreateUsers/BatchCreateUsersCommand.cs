using Mentoory.Access.Application.Commands.BatchRegisterUsers;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Commands.BatchCreateUsers;

public sealed record BatchCreateUsersCommand(
    IReadOnlyList<BatchUserRow> Rows,
    bool SkipEmailVerification,
    bool SkipInvitationAcceptance,
    Guid ProjectExternalId,
    long CreatedByUserId) : IBaseRequest<BatchCreateUsersResult>;
