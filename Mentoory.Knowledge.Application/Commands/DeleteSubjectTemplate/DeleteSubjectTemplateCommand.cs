using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.DeleteSubjectTemplate;

/// <summary>
/// Represents a command to delete a subject from a knowledge structure template.
/// </summary>
/// <param name="TemplateExternalId">The external identifier of the knowledge structure template.</param>
/// <param name="SubjectExternalId">The external identifier of the subject to delete.</param>
public sealed record DeleteSubjectTemplateCommand(
    Guid TemplateExternalId,
    Guid SubjectExternalId) : IBaseRequest;
