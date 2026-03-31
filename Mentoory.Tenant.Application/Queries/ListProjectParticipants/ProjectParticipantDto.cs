namespace Mentoory.Tenant.Application.Queries.ListProjectParticipants;

/// <summary>
/// Data transfer object for project participant list items.
/// </summary>
public sealed record ProjectParticipantDto(
    Guid ExternalId,
    long UserId,
    string Role,
    bool IsActive,
    DateTime EnrolledAtUtc);
