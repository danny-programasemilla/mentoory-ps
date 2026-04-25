using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Commands.AdminEnrollUser;

/// <summary>
/// Admin-initiated user enrollment. Duplicate-identity outcomes surface as field-attributed
/// validation errors (<c>Email</c> / <c>NationalId</c>) so the admin can resolve conflicts.
/// </summary>
public sealed record AdminEnrollUserCommand(
    string Email,
    string Country,
    string NationalId,
    string FirstName,
    string LastName,
    string Password) : IBaseRequest;
