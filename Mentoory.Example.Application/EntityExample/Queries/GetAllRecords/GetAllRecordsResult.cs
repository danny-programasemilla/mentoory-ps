namespace Mentoory.Example.Application.EntityExample.Queries.GetAllRecords;

/// <summary>
/// Represents the result of retrieving all records from the system.
/// </summary>
/// <param name="Records">The collection of string records retrieved from the data source.</param>
public record GetAllRecordsResult(IEnumerable<string> Records);
