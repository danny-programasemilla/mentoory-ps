using Mentoory.Access.Domain.Enums;
using Mentoory.Access.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Microsoft.EntityFrameworkCore;

namespace Mentoory.Access.Application.Users.Queries.ListUsersByStatus;

/// <summary>
/// Handles the ListUsersByStatusQuery by querying User aggregates
/// with optional AccountStatus and search term filters.
/// </summary>
public class ListUsersByStatusHandler(IUserRepository userRepository)
    : BaseCommandHandler<ListUsersByStatusQuery, List<UserSummaryDto>>
{
    /// <inheritdoc />
    public override async Task<Result<List<UserSummaryDto>>> Handle(
        ListUsersByStatusQuery request,
        CancellationToken cancellationToken)
    {
        var query = userRepository.Query();

        if (!string.IsNullOrWhiteSpace(request.AccountStatus)
            && Enum.TryParse<AccountStatus>(request.AccountStatus.Trim(), out var statusEnum))
        {
            query = query.Where(u => u.AccountStatus == statusEnum);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.ToUpperInvariant();
            query = query.Where(u =>
                u.Email.NormalizedValue.Contains(search) ||
                u.FirstName.ToUpper().Contains(search) ||
                u.LastName.ToUpper().Contains(search));
        }

        var results = await query
            .Select(u => new UserSummaryDto(
                u.ExternalId,
                u.Email.Value,
                u.FirstName,
                u.LastName,
                u.AccountStatus.ToString(),
                u.CreatedAtUtc))
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync(cancellationToken);

        return Success(results);
    }
}
