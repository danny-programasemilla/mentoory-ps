using Mentoory.Access.Application.Commands.RegisterUser;
using Mentoory.Shared.Application.Audit;

namespace Mentoory.Access.Application.Audit;

/// <summary>
/// Supplies the user email to the audit pipeline when the tenant context is empty
/// (anonymous registration flow). User id is null because the row does not yet exist.
/// </summary>
internal sealed class RegisterUserAuditResolver : IAuditAnonymousResolver<RegisterUserCommand>
{
    public (long? UserId, string? UserEmail) Resolve(RegisterUserCommand request) => (null, request.Email);
}
