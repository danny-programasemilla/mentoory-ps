using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Diagnostic.Application.Queries.GetProjectForm;

/// <summary>
/// Query to retrieve a project form by its external identifier, including questions.
/// </summary>
/// <param name="ExternalId">The external GUID identifier of the project form.</param>
public sealed record GetProjectFormQuery(Guid ExternalId, long? ProjectId = null) : IBaseRequest<ProjectFormDto?>;
