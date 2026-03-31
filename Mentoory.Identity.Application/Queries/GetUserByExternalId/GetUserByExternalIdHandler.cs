using Mentoory.Identity.Domain.Aggregates.User;
using Mentoory.Identity.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Identity.Application.Queries.GetUserByExternalId;

/// <summary>
/// Handles the query to retrieve a user by their external identifier.
/// </summary>
public class GetUserByExternalIdHandler : BaseCommandHandler<GetUserByExternalIdQuery, User?>
{
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetUserByExternalIdHandler"/> class.
    /// </summary>
    /// <param name="userRepository">The user repository for data access.</param>
    public GetUserByExternalIdHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    /// <inheritdoc />
    public override async Task<Result<User?>> Handle(GetUserByExternalIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByExternalIdAsync(request.ExternalId, cancellationToken);
        return Success(user);
    }
}
