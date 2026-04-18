using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.CloneKnowledgeStructureTemplate;

/// <summary>
/// Represents a command to clone a knowledge structure template into a new project-scoped structure.
/// </summary>
/// <param name="SourceTemplateExternalId">The external identifier of the template to clone from.</param>
/// <param name="ProjectId">The identifier of the project that will own the clone.</param>
/// <param name="IncubatorId">The identifier of the incubator that owns the project.</param>
public sealed record CloneKnowledgeStructureTemplateCommand(
    Guid SourceTemplateExternalId,
    long ProjectId,
    long IncubatorId) : IBaseRequest<Guid>;
