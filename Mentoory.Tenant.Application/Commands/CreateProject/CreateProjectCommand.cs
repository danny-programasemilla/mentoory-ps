using Mentoory.Shared.Application.MediatR;
using Mentoory.Tenant.Domain.Enums;

namespace Mentoory.Tenant.Application.Commands.CreateProject;

public sealed record CreateProjectCommand(
    Guid IncubatorExternalId,
    string Name,
    string? Description,
    Guid KnowledgeStructureTemplateExternalId,
    bool IsPublic = false,
    EnrollmentVariant EnrollmentVariant = EnrollmentVariant.FullFlow) : IBaseRequest<Guid>;
