using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Tenant.Application.Projects.Queries.ListPublicProjects;

public sealed record ListPublicProjectsQuery() : IBaseRequest<List<PublicProjectDto>>;

public sealed record PublicProjectDto(Guid ExternalId, string Name, string? Description, string IncubatorName);
