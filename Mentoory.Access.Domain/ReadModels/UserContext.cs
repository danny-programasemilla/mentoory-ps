namespace Mentoory.Access.Domain.ReadModels;

public record UserContext(
    Guid RoleAssignmentExternalId,
    long UserId,
    long IncubatorId,
    string? IncubatorName,
    long? ProjectId,
    string? ProjectName,
    string Role);
