using Mentoory.Identity.Domain.Aggregates.User;
using Mentoory.Identity.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Identity.Application.Queries.GetUserByEmail;

/// <summary>
/// Handles the query to retrieve a user by their email address.
/// </summary>
public class GetUserByEmailHandler : BaseCommandHandler<GetUserByEmailQuery, User?>
{
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetUserByEmailHandler"/> class.
    /// </summary>
    /// <param name="userRepository">The user repository for data access.</param>
    public GetUserByEmailHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    /// <inheritdoc />
    public override async Task<Result<User?>> Handle(GetUserByEmailQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.NormalizedEmail, cancellationToken);
        return Success(user);
    }
}
