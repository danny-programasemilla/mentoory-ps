using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.DeleteSubject;

/// <summary>
/// Represents a command to remove a subject from a knowledge structure (project clone).
/// </summary>
/// <param name="StructureExternalId">The external identifier of the knowledge structure.</param>
/// <param name="SubjectExternalId">The external identifier of the subject to remove.</param>
public sealed record DeleteSubjectCommand(
    Guid StructureExternalId,
    Guid SubjectExternalId) : IBaseRequest;
