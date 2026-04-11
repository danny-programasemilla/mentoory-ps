namespace Mentoory.Shared.Application.DataTables;

public sealed record DataTableRequest(
    int Draw,
    int Start,
    int Length,
    string? SortColumn,
    string SortDirection,
    string? SearchValue,
    Dictionary<string, string>? Filters);
