using System.Linq.Expressions;
using LinaSys.Shared.Application.Extensions;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.DataTables;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Tenant.Domain.Aggregates.Incubator;
using Mentoory.Tenant.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Mentoory.Tenant.Application.Queries.ListIncubators;

/// <summary>
/// Handler for listing incubators with DataTable paging and sorting.
/// </summary>
/// <param name="logger">Logger instance for tracking operations.</param>
/// <param name="repository">The repository for accessing incubator entities.</param>
public partial class ListIncubatorsHandler(
    ILogger<ListIncubatorsHandler> logger,
    IIncubatorRepository repository)
    : BaseCommandHandler<ListIncubatorsQuery, DataTableResponse<IncubatorDto>>
{
    private static readonly Dictionary<string, Expression<Func<Incubator, object?>>> SortColumns = new()
    {
        ["name"] = i => i.Name,
        ["createdatutc"] = i => i.CreatedAtUtc,
        ["isactive"] = i => i.IsActive,
    };

    /// <inheritdoc />
    public override async Task<Result<DataTableResponse<IncubatorDto>>> Handle(
        ListIncubatorsQuery request,
        CancellationToken cancellationToken)
    {
        var dt = request.DataTableRequest;
        var query = repository.Query();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(dt.SearchValue))
        {
            var search = dt.SearchValue.ToLowerInvariant();
            query = query.Where(i => i.Name.ToLower().Contains(search));
        }

        if (dt.Filters is { Count: > 0 })
        {
            if (dt.Filters.TryGetValue("name", out var nameFilter) && !string.IsNullOrEmpty(nameFilter))
            {
                query = query.Where(i => i.Name.ToLower().Contains(nameFilter.ToLowerInvariant()));
            }

            if (dt.Filters.TryGetValue("description", out var descFilter) && !string.IsNullOrEmpty(descFilter))
            {
                query = query.Where(i => i.Description != null && i.Description.ToLower().Contains(descFilter.ToLowerInvariant()));
            }

            if (dt.Filters.TryGetValue("isActive", out var activeFilter) && !string.IsNullOrEmpty(activeFilter)
                && bool.TryParse(activeFilter, out var isActive))
            {
                query = query.Where(i => i.IsActive == isActive);
            }
        }

        var totalCount = await repository.CountAsync(cancellationToken);
        var filteredCount = await repository.CountAsync(query, cancellationToken);

        // Apply ordering and paging
        query = query
            .ApplyOrdering(dt.SortColumn, dt.SortDirection, SortColumns)
            .ApplyPaging(dt.Start, dt.Length);

        var items = await repository.ToListAsync(
            query.Select(i => new IncubatorDto(
                i.ExternalId,
                i.Name,
                i.Description,
                i.IsActive,
                i.CreatedAtUtc,
                i.UpdatedAtUtc)),
            cancellationToken);

        var response = new DataTableResponse<IncubatorDto>(
            dt.Draw,
            totalCount,
            filteredCount,
            items);

        LogIncubatorsListed(items.Count);

        return Success(response);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Listed {Count} incubators")]
    partial void LogIncubatorsListed(int count);
}
