namespace Mentoory.Access.Application.Commands.BatchRegisterUsers;

public sealed record BatchUserRow(
    string Country,
    string Identification,
    string Email,
    string FirstName,
    string LastName);
