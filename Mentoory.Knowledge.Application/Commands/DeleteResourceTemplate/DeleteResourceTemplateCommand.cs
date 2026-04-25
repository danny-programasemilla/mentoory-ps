using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.DeleteResourceTemplate;

/// <summary>
/// Represents a command to delete a resource from a knowledge structure template.
/// </summary>
/// <param name="TemplateExternalId">The external identifier of the knowledge structure template.</param>
/// <param name="ResourceExternalId">The external identifier of the resource to delete.</param>
public sealed record DeleteResourceTemplateCommand(
    Guid TemplateExternalId,
    Guid ResourceExternalId) : IBaseRequest;
