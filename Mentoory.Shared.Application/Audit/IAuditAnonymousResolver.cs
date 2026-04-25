namespace Mentoory.Shared.Application.Audit;

/// <summary>
/// Provides UserId/UserEmail values for anonymous commands (register, login) whose
/// tenant context is empty at the time the <c>AuditingBehavior</c> runs.
/// Implementations are scoped-registered per command type.
/// </summary>
/// <typeparam name="TRequest">The anonymous command type this resolver handles.</typeparam>
public interface IAuditAnonymousResolver<in TRequest>
{
    (long? UserId, string? UserEmail) Resolve(TRequest request);
}
