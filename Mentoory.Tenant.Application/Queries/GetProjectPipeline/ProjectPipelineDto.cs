namespace Mentoory.Tenant.Application.Queries.GetProjectPipeline;

public sealed record ProjectPipelineDto(
    Guid ProjectExternalId,
    string ProjectName,
    string CurrentStageState,
    IReadOnlyList<PipelineStageDto> Stages);

public sealed record PipelineStageDto(
    long StageId,
    Guid ExternalId,
    string StageType,
    string State,
    int Position,
    string DisplayName,
    DateTime? PlannedStartDate,
    DateTime? PlannedEndDate,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc);
