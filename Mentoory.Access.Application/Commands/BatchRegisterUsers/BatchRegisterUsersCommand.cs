using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Commands.BatchRegisterUsers;

public sealed record BatchRegisterUsersCommand(
    IReadOnlyList<BatchUserRow> Rows,
    Guid ProjectExternalId,
    Guid IncubatorExternalId,
    long CreatedByUserId) : IBaseRequest<BatchRegistrationResult>;
