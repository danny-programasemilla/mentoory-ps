using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.UpdateSubjectTemplate;

/// <summary>
/// Represents a command to update the details of a subject within a knowledge structure template.
/// </summary>
/// <param name="TemplateExternalId">The external identifier of the knowledge structure template.</param>
/// <param name="SubjectExternalId">The external identifier of the subject to update.</param>
/// <param name="Name">The new name of the subject.</param>
/// <param name="Description">The new optional description for the subject.</param>
public sealed record UpdateSubjectTemplateCommand(
    Guid TemplateExternalId,
    Guid SubjectExternalId,
    string Name,
    string? Description) : IBaseRequest;
