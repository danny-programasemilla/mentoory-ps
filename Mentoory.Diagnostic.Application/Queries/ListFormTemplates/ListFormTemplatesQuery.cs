using Mentoory.Shared.Application.DataTables;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Diagnostic.Application.Queries.ListFormTemplates;

/// <summary>
/// Query to retrieve a paginated list of form templates for DataTable display.
/// </summary>
/// <param name="Request">The DataTable request parameters including paging, sorting, and filtering.</param>
/// <param name="SubscriptionTier">Optional filter by subscription tier.</param>
public sealed record ListFormTemplatesQuery(
    DataTableRequest Request,
    string? SubscriptionTier) : IBaseRequest<DataTableResponse<FormTemplateListItemDto>>;
