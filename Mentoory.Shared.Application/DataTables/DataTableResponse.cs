namespace Mentoory.Shared.Application.DataTables;

public sealed record DataTableResponse<T>(
    int Draw,
    int RecordsTotal,
    int RecordsFiltered,
    IReadOnlyList<T> Data);
