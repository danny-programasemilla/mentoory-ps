using System.Linq.Expressions;
using LinaSys.Shared.Application.Extensions;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.DataTables;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Tenant.Domain.Aggregates.Project;
using Mentoory.Tenant.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Mentoory.Tenant.Application.Queries.ListProjects;

/// <summary>
/// Handler for listing projects with DataTable paging, sorting, and optional incubator filter.
/// </summary>
/// <param name="logger">Logger instance for tracking operations.</param>
/// <param name="repository">The repository for accessing project entities.</param>
public partial class ListProjectsHandler(
    ILogger<ListProjectsHandler> logger,
    IProjectRepository repository)
    : BaseCommandHandler<ListProjectsQuery, DataTableResponse<ProjectDto>>
{
    private static readonly Dictionary<string, Expression<Func<Project, object?>>> SortColumns = new()
    {
        ["name"] = p => p.Name,
        ["createdatutc"] = p => p.CreatedAtUtc,
        ["isactive"] = p => p.IsActive,
        ["currentstagetype"] = p => p.CurrentStageType,
    };

    /// <inheritdoc />
    public override async Task<Result<DataTableResponse<ProjectDto>>> Handle(
        ListProjectsQuery request,
        CancellationToken cancellationToken)
    {
        var dt = request.DataTableRequest;
        var query = repository.Query();

        // Apply incubator filter
        if (request.IncubatorId.HasValue)
        {
            query = query.Where(p => p.IncubatorId == request.IncubatorId.Value);
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(dt.SearchValue))
        {
            var search = dt.SearchValue.ToLowerInvariant();
            query = query.Where(p => p.Name.ToLower().Contains(search));
        }

        if (dt.Filters is { Count: > 0 })
        {
            if (dt.Filters.TryGetValue("name", out var nameFilter) && !string.IsNullOrEmpty(nameFilter))
            {
                query = query.Where(p => p.Name.ToLower().Contains(nameFilter.ToLowerInvariant()));
            }

            if (dt.Filters.TryGetValue("description", out var descFilter) && !string.IsNullOrEmpty(descFilter))
            {
                query = query.Where(p => p.Description != null && p.Description.ToLower().Contains(descFilter.ToLowerInvariant()));
            }

            if (dt.Filters.TryGetValue("currentStageType", out var stageFilter) && !string.IsNullOrEmpty(stageFilter))
            {
                query = query.Where(p => p.CurrentStageType.ToString().ToLower().Contains(stageFilter.ToLowerInvariant()));
            }

            if (dt.Filters.TryGetValue("isActive", out var activeFilter) && !string.IsNullOrEmpty(activeFilter)
                && bool.TryParse(activeFilter, out var isActive))
            {
                query = query.Where(p => p.IsActive == isActive);
            }
        }

        var totalCount = await repository.CountAsync(cancellationToken);
        var filteredCount = await repository.CountAsync(query, cancellationToken);

        // Apply ordering and paging
        query = query
            .ApplyOrdering(dt.SortColumn, dt.SortDirection, SortColumns)
            .ApplyPaging(dt.Start, dt.Length);

        var items = await repository.ToListAsync(
            query.Select(p => new ProjectDto(
                p.ExternalId,
                p.IncubatorId,
                p.Name,
                p.Description,
                p.CurrentStageType.ToString(),
                p.CurrentStageState.ToString(),
                p.IsActive,
                p.CreatedAtUtc,
                p.UpdatedAtUtc)),
            cancellationToken);

        var response = new DataTableResponse<ProjectDto>(
            dt.Draw,
            totalCount,
            filteredCount,
            items);

        LogProjectsListed(items.Count);

        return Success(response);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Listed {Count} projects")]
    partial void LogProjectsListed(int count);
}
