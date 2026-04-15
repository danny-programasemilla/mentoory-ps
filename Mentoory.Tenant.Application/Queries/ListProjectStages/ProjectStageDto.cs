namespace Mentoory.Tenant.Application.Queries.ListProjectStages;

public sealed record ProjectStageDto(
    Guid ExternalId,
    string StageType,
    string State,
    int Position,
    string DisplayName);
