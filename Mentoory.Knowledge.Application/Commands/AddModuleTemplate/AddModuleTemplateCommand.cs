using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.AddModuleTemplate;

/// <summary>
/// Represents a command to add a new module to a knowledge structure template.
/// </summary>
/// <param name="TemplateExternalId">The external identifier of the knowledge structure template.</param>
/// <param name="Name">The name of the module.</param>
/// <param name="Description">An optional description for the module.</param>
/// <param name="SortOrder">The sort order of the module within the template.</param>
public sealed record AddModuleTemplateCommand(
    Guid TemplateExternalId,
    string Name,
    string? Description,
    int SortOrder) : IBaseRequest<Guid>;
