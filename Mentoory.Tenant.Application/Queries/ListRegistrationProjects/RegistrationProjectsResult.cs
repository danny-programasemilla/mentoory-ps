namespace Mentoory.Tenant.Application.Queries.ListRegistrationProjects;

public sealed record RegistrationProjectsResult(
    Guid IncubatorExternalId,
    List<RegistrationProjectDto> Projects);
