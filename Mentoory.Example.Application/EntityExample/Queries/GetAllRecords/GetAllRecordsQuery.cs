using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Example.Application.EntityExample.Queries.GetAllRecords;

/// <summary>
/// Query to retrieve all example records.
/// </summary>
public sealed record GetAllRecordsQuery : IBaseRequest<List<Domain.Aggregates.Example.Example>>;
