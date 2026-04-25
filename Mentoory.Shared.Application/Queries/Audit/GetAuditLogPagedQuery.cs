using Mentoory.Shared.Application.DataTables;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Shared.Application.Queries.Audit;

/// <summary>
/// Server-side paged query for <c>[audit].[AuditLog]</c>, consumed by the
/// <c>/Administration/AuditLog</c> DataTable. Filter values arrive via
/// <see cref="DataTableRequest.Filters"/> keyed by the DTO column name:
/// <c>eventType</c>, <c>userEmail</c>, <c>outcome</c>, <c>fromUtc</c>, <c>toUtc</c>.
/// </summary>
public sealed record GetAuditLogPagedQuery(DataTableRequest Request)
    : IBaseRequest<DataTableResponse<AuditLogDto>>;
