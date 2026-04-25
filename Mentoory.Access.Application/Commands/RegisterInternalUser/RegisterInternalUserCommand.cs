using Mentoory.Shared.Application.Audit;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Commands.RegisterInternalUser;

[Audited(AuditEventTypes.UserRegistered, EntityType = "User")]
public sealed record RegisterInternalUserCommand(
    string Country,
    string Identification,
    string Email,
    string Password,
    bool RequireEmailVerification,
    Guid ProjectExternalId) : IBaseRequest<RegisterInternalUserResult>;
