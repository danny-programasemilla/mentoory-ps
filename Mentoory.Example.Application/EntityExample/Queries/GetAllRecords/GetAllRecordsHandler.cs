using Mentoory.Example.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Example.Application.EntityExample.Queries.GetAllRecords;

/// <summary>
/// Handler for the GetAllRecordsQuery that retrieves all example records from the repository.
/// </summary>
/// <param name="exampleRepository">The repository for accessing example data.</param>
public class GetAllRecordsHandler(IExampleRepository exampleRepository)
: BaseCommandHandler<GetAllRecordsQuery, List<Domain.Aggregates.Example.Example>>
{
    /// <summary>
    /// Handles the query to retrieve all example records.
    /// </summary>
    /// <param name="request">The query request.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of all example records.</returns>
    public override async Task<Result<List<Domain.Aggregates.Example.Example>>> Handle(GetAllRecordsQuery request, CancellationToken cancellationToken)
    {
        var records = await exampleRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return Success(records);
    }
}
