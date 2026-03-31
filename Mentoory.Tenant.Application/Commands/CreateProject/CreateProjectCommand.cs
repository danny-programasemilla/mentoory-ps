using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Tenant.Application.Commands.CreateProject;

public sealed record CreateProjectCommand(Guid IncubatorExternalId, string Name, string? Description) : IBaseRequest<Guid>;
