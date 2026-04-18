using Mentoory.Access.Application.Commands.LoginUser;
using Mentoory.Shared.Application.Audit;

namespace Mentoory.Access.Application.Audit;

/// <summary>
/// Supplies the user email to the audit pipeline when the tenant context is empty
/// (pre-authentication login flow). User id is null because authentication has not
/// completed at the time the audit row is written.
/// </summary>
internal sealed class LoginUserAuditResolver : IAuditAnonymousResolver<LoginUserCommand>
{
    public (long? UserId, string? UserEmail) Resolve(LoginUserCommand request) => (null, request.Email);
}
