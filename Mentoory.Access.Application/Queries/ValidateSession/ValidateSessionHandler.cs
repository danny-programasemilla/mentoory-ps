using Mentoory.Access.Domain.Aggregates.AuthSession;
using Mentoory.Access.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;

namespace Mentoory.Access.Application.Queries.ValidateSession;

/// <summary>
/// Handles session validation by checking the token and its expiration.
/// </summary>
public class ValidateSessionHandler : BaseCommandHandler<ValidateSessionQuery, AuthSession?>
{
    private readonly IAuthSessionRepository _authSessionRepository;
    private readonly ITimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidateSessionHandler"/> class.
    /// </summary>
    /// <param name="authSessionRepository">The auth session repository for data access.</param>
    /// <param name="timeProvider">The time provider for obtaining current UTC time.</param>
    public ValidateSessionHandler(
        IAuthSessionRepository authSessionRepository,
        ITimeProvider timeProvider)
    {
        _authSessionRepository = authSessionRepository;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public override async Task<Result<AuthSession?>> Handle(ValidateSessionQuery request, CancellationToken cancellationToken)
    {
        var session = await _authSessionRepository.GetByTokenAsync(request.SessionToken, cancellationToken);
        if (session is null || !session.IsValid(_timeProvider.UtcNow))
        {
            return Success((AuthSession?)null);
        }

        session.UpdateActivity(_timeProvider.UtcNow);
        _authSessionRepository.Update(session);
        await _authSessionRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return Success((AuthSession?)session);
    }
}
