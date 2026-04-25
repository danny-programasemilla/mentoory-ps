using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.DeleteModuleTemplate;

/// <summary>
/// Represents a command to remove a module from a knowledge structure template.
/// </summary>
/// <param name="TemplateExternalId">The external identifier of the knowledge structure template.</param>
/// <param name="ModuleExternalId">The external identifier of the module to remove.</param>
public sealed record DeleteModuleTemplateCommand(
    Guid TemplateExternalId,
    Guid ModuleExternalId) : IBaseRequest;
