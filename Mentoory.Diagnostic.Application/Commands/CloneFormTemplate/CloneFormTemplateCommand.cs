using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Diagnostic.Application.Commands.CloneFormTemplate;

/// <summary>
/// Represents a command to clone a form template into a project-specific form.
/// </summary>
/// <param name="SourceTemplateExternalId">The external identifier of the source form template.</param>
/// <param name="ProjectId">The project to associate the cloned form with.</param>
/// <param name="IncubatorId">The incubator that owns the project.</param>
public sealed record CloneFormTemplateCommand(
    Guid SourceTemplateExternalId,
    long ProjectId,
    long IncubatorId) : IBaseRequest;
