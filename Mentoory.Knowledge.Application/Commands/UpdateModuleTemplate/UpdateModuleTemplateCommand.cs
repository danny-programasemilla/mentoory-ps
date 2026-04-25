using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.UpdateModuleTemplate;

/// <summary>
/// Represents a command to update an existing module within a knowledge structure template.
/// </summary>
/// <param name="TemplateExternalId">The external identifier of the knowledge structure template.</param>
/// <param name="ModuleExternalId">The external identifier of the module to update.</param>
/// <param name="Name">The new name of the module.</param>
/// <param name="Description">An optional description for the module.</param>
public sealed record UpdateModuleTemplateCommand(
    Guid TemplateExternalId,
    Guid ModuleExternalId,
    string Name,
    string? Description) : IBaseRequest;
