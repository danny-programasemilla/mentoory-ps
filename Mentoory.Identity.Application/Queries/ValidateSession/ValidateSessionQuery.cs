using Mentoory.Identity.Domain.Aggregates.AuthSession;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Identity.Application.Queries.ValidateSession;

/// <summary>
/// Query to validate an authentication session by its token.
/// </summary>
/// <param name="SessionToken">The session token to validate.</param>
public sealed record ValidateSessionQuery(string SessionToken) : IBaseRequest<AuthSession?>;
