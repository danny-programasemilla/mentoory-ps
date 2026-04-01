using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Diagnostic.Application.Commands.SyncFromTemplate;

/// <summary>
/// Represents a command to synchronize new questions from the source template into a project form.
/// </summary>
/// <param name="ProjectFormExternalId">The external identifier of the project form to sync.</param>
public sealed record SyncFromTemplateCommand(
    Guid ProjectFormExternalId) : IBaseRequest;
