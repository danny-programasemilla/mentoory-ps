using Mentoory.Shared.Application.DataTables;

namespace Mentoory.Web.Models;

/// <summary>
/// Binds the DataTables jQuery plugin server-side request parameters to a strongly-typed model.
/// </summary>
public sealed class DataTableServerRequest
{
    public int Draw { get; set; }

    public int Start { get; set; }

    public int Length { get; set; }

    public DataTableSearchValue? Search { get; set; }

    public List<DataTableOrderValue>? Order { get; set; }

    public Dictionary<string, string>? Filters { get; set; }

    public DataTableRequest ToDataTableRequest()
    {
        var sortColumn = Order is { Count: > 0 } ? Order[0].Column.ToString() : null;
        var sortDirection = Order is { Count: > 0 } ? Order[0].Dir : "asc";
        var searchValue = Search?.Value;

        return new DataTableRequest(Draw, Start, Length, sortColumn, sortDirection, searchValue, Filters);
    }
}

public sealed class DataTableSearchValue
{
    public string? Value { get; set; }
}

public sealed class DataTableOrderValue
{
    public int Column { get; set; }

    public string Dir { get; set; } = "asc";
}
