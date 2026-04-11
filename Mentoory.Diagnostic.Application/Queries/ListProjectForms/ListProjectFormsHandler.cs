using System.Linq.Expressions;
using LinaSys.Shared.Application.Extensions;
using Mentoory.Diagnostic.Domain.Aggregates.ProjectForm;
using Mentoory.Diagnostic.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.DataTables;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Diagnostic.Application.Queries.ListProjectForms;

/// <summary>
/// Handles the ListProjectFormsQuery by querying project forms for a specific project.
/// </summary>
public class ListProjectFormsHandler : BaseCommandHandler<ListProjectFormsQuery, DataTableResponse<ProjectFormListItemDto>>
{
    private static readonly Dictionary<string, Expression<Func<ProjectForm, object?>>> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        { "name", x => x.Name },
        { "createdAtUtc", x => x.CreatedAtUtc },
    };

    private readonly IProjectFormRepository _projectFormRepository;

    public ListProjectFormsHandler(IProjectFormRepository projectFormRepository)
    {
        _projectFormRepository = projectFormRepository;
    }

    /// <inheritdoc />
    public override async Task<Result<DataTableResponse<ProjectFormListItemDto>>> Handle(ListProjectFormsQuery request, CancellationToken cancellationToken)
    {
        var dt = request.Request;
        var query = _projectFormRepository.Query()
            .Where(f => f.ProjectId == request.ProjectId);

        var totalRecords = await _projectFormRepository.CountAsync(query, cancellationToken);

        if (!string.IsNullOrWhiteSpace(dt.SearchValue))
        {
            var search = dt.SearchValue.ToUpperInvariant();
            query = query.Where(f => f.Name.ToUpper().Contains(search));
        }

        var filteredCount = await _projectFormRepository.CountAsync(query, cancellationToken);

        query = query
            .ApplyOrdering(dt.SortColumn, dt.SortDirection, SortColumns)
            .ApplyPaging(dt.Start, dt.Length);

        var items = await _projectFormRepository.ToListAsync(
            query.Select(f => new ProjectFormListItemDto(
                f.ExternalId,
                f.Name,
                f.SyncMode,
                f.Questions.Count,
                f.CreatedAtUtc)),
            cancellationToken);

        return Success(new DataTableResponse<ProjectFormListItemDto>(
            dt.Draw, totalRecords, filteredCount, items));
    }
}
