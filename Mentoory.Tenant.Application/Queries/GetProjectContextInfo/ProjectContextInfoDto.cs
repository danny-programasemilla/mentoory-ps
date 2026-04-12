namespace Mentoory.Tenant.Application.Queries.GetProjectContextInfo;

public sealed record ProjectContextInfoDto(
    Guid ProjectExternalId,
    Guid IncubatorExternalId);
