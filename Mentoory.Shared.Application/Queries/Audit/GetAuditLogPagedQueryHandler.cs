using System.Globalization;
using System.Linq.Expressions;
using LinaSys.Shared.Application.Extensions;
using Mentoory.Shared.Application.DataTables;
using Mentoory.Shared.Application.MediatR;
using Microsoft.EntityFrameworkCore;

namespace Mentoory.Shared.Application.Queries.Audit;

/// <summary>
/// Resolves the admin viewer query against the audit read surface.
/// Filter values arrive via <see cref="DataTableRequest.Filters"/> keyed by
/// <c>eventType</c>, <c>userEmail</c>, <c>outcome</c>, <c>fromUtc</c>, <c>toUtc</c>.
/// Unrecognized filter values are silently ignored (the validator rejects the
/// request before reaching here for the semantic cases).
/// </summary>
public sealed class GetAuditLogPagedQueryHandler
    : BaseCommandHandler<GetAuditLogPagedQuery, DataTableResponse<AuditLogDto>>
{
    private static readonly Dictionary<string, Expression<Func<AuditLogReadEntity, object?>>> SortColumns =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { "occurredAtUtc", x => x.OccurredAtUtc },
            { "eventType", x => x.EventType },
            { "userEmail", x => x.UserEmail },
            { "outcome", x => x.Outcome },
            { "action", x => x.Action },
            { "roleContext", x => x.RoleContext },
        };

    private readonly IAuditLogReadRepository _repository;

    public GetAuditLogPagedQueryHandler(IAuditLogReadRepository repository)
    {
        _repository = repository;
    }

    public override async Task<Result<DataTableResponse<AuditLogDto>>> Handle(
        GetAuditLogPagedQuery request,
        CancellationToken cancellationToken)
    {
        var req = request.Request;
        var query = _repository.Query();

        var totalRecords = await query.CountAsync(cancellationToken);

        query = ApplyFilters(query, req.Filters);

        var filteredRecords = await query.CountAsync(cancellationToken);

        var sortColumn = req.SortColumn ?? "occurredAtUtc";
        var sortDirection = string.IsNullOrWhiteSpace(req.SortDirection) ? "desc" : req.SortDirection;

        var paged = query
            .ApplyOrdering(sortColumn, sortDirection, SortColumns)
            .ApplyPaging(req.Start, req.Length);

        var data = await paged
            .Select(row => new AuditLogDto(
                row.Id,
                row.OccurredAtUtc,
                row.EventType,
                row.Action,
                row.Outcome,
                row.UserEmail,
                row.RoleContext,
                row.UserId,
                row.IncubatorId,
                row.ProjectId,
                row.EntityType,
                row.EntityId,
                row.CorrelationId,
                row.ExceptionType,
                row.IpAddress,
                row.Details))
            .ToListAsync(cancellationToken);

        return Success(new DataTableResponse<AuditLogDto>(
            req.Draw,
            totalRecords,
            filteredRecords,
            data));
    }

    private static IQueryable<AuditLogReadEntity> ApplyFilters(
        IQueryable<AuditLogReadEntity> query,
        Dictionary<string, string>? filters)
    {
        if (filters is null || filters.Count == 0)
        {
            return query;
        }

        if (filters.TryGetValue("eventType", out var eventType) && !string.IsNullOrWhiteSpace(eventType))
        {
            query = query.Where(r => r.EventType == eventType);
        }

        if (filters.TryGetValue("userEmail", out var userEmail) && !string.IsNullOrWhiteSpace(userEmail))
        {
            var needle = userEmail.ToUpperInvariant();
            query = query.Where(r => r.UserEmail != null && r.UserEmail.ToUpper().Contains(needle));
        }

        if (filters.TryGetValue("outcome", out var outcome) && !string.IsNullOrWhiteSpace(outcome))
        {
            query = query.Where(r => r.Outcome == outcome);
        }

        if (TryParseUtc(filters, "fromUtc", out var fromUtc))
        {
            query = query.Where(r => r.OccurredAtUtc >= fromUtc);
        }

        if (TryParseUtc(filters, "toUtc", out var toUtc))
        {
            query = query.Where(r => r.OccurredAtUtc <= toUtc);
        }

        return query;
    }

    private static bool TryParseUtc(Dictionary<string, string> filters, string key, out DateTime value)
    {
        if (filters.TryGetValue(key, out var raw)
            && !string.IsNullOrWhiteSpace(raw)
            && DateTime.TryParse(
                raw,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out value))
        {
            return true;
        }

        value = default;
        return false;
    }
}
