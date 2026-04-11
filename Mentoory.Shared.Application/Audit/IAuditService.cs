namespace Mentoory.Shared.Application.Audit;

public interface IAuditService
{
    Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default);
}
