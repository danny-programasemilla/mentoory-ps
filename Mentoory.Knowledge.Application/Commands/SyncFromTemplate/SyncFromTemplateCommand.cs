using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.SyncFromTemplate;

/// <summary>
/// Represents a command to partially sync a knowledge structure (project clone)
/// with its source template, pulling in any template-side items that are missing locally
/// while leaving local additions and renames untouched.
/// </summary>
/// <param name="StructureExternalId">The external identifier of the knowledge structure to sync.</param>
public sealed record SyncFromTemplateCommand(Guid StructureExternalId) : IBaseRequest<PartialSyncResultDto>;

/// <summary>
/// Per-level counts returned by a successful partial sync.
/// </summary>
/// <param name="ModulesAdded">Number of modules appended from the template.</param>
/// <param name="TopicsAdded">Number of topics appended from the template.</param>
/// <param name="SubjectsAdded">Number of subjects appended from the template.</param>
/// <param name="ResourcesAdded">Number of resources appended from the template.</param>
public sealed record PartialSyncResultDto(
    int ModulesAdded,
    int TopicsAdded,
    int SubjectsAdded,
    int ResourcesAdded);
