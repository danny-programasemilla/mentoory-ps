using System.Linq.Expressions;
using LinaSys.Shared.Application.Extensions;
using Mentoory.Diagnostic.Domain.Aggregates.FormTemplate;
using Mentoory.Diagnostic.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.DataTables;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Diagnostic.Application.Queries.ListFormTemplates;

/// <summary>
/// Handles the ListFormTemplatesQuery by querying the database for a paginated list of form templates.
/// </summary>
public class ListFormTemplatesHandler : BaseCommandHandler<ListFormTemplatesQuery, DataTableResponse<FormTemplateListItemDto>>
{
    private static readonly Dictionary<string, Expression<Func<FormTemplate, object?>>> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        { "name", x => x.Name },
        { "subscriptionTier", x => x.SubscriptionTier! },
        { "version", x => x.Version },
        { "isActive", x => x.IsActive },
        { "createdAtUtc", x => x.CreatedAtUtc },
    };

    private readonly IFormTemplateRepository _formTemplateRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListFormTemplatesHandler"/> class.
    /// </summary>
    public ListFormTemplatesHandler(IFormTemplateRepository formTemplateRepository)
    {
        _formTemplateRepository = formTemplateRepository;
    }

    /// <inheritdoc />
    public override async Task<Result<DataTableResponse<FormTemplateListItemDto>>> Handle(ListFormTemplatesQuery request, CancellationToken cancellationToken)
    {
        var dt = request.Request;
        var query = _formTemplateRepository.Query();

        var totalRecords = await _formTemplateRepository.CountAsync(cancellationToken);

        // Apply subscription tier filter
        if (!string.IsNullOrWhiteSpace(request.SubscriptionTier))
        {
            var tier = request.SubscriptionTier;
            query = query.Where(t => t.SubscriptionTier == tier);
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(dt.SearchValue))
        {
            var search = dt.SearchValue.ToUpperInvariant();
            query = query.Where(t =>
                t.Name.ToUpper().Contains(search) ||
                (t.Description != null && t.Description.ToUpper().Contains(search)));
        }

        var filteredCount = await _formTemplateRepository.CountAsync(query, cancellationToken);

        // Apply ordering and paging
        query = query
            .ApplyOrdering(dt.SortColumn, dt.SortDirection, SortColumns)
            .ApplyPaging(dt.Start, dt.Length);

        // Project to DTO
        var items = await _formTemplateRepository.ToListAsync(
            query.Select(t => new FormTemplateListItemDto(
                t.ExternalId,
                t.Name,
                t.Description,
                t.SubscriptionTier,
                t.Version,
                t.IsActive,
                t.CreatedAtUtc)),
            cancellationToken);

        var response = new DataTableResponse<FormTemplateListItemDto>(
            dt.Draw,
            totalRecords,
            filteredCount,
            items);

        return Success(response);
    }
}
