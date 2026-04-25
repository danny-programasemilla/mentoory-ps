using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.UpdateSubject;

/// <summary>
/// Represents a command to update a subject within a knowledge structure (project clone).
/// </summary>
/// <param name="StructureExternalId">The external identifier of the knowledge structure.</param>
/// <param name="SubjectExternalId">The external identifier of the subject to update.</param>
/// <param name="Name">The new name of the subject.</param>
/// <param name="Description">The new optional description for the subject.</param>
public sealed record UpdateSubjectCommand(
    Guid StructureExternalId,
    Guid SubjectExternalId,
    string Name,
    string? Description) : IBaseRequest;
