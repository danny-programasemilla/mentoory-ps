using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Diagnostic.Application.Queries.GetDiagnosticResponse;

/// <summary>
/// Query to retrieve a diagnostic response by its external identifier, including question responses and corrections.
/// </summary>
/// <param name="ExternalId">The external GUID identifier of the diagnostic response.</param>
public sealed record GetDiagnosticResponseQuery(Guid ExternalId, long? ProjectId = null) : IBaseRequest<DiagnosticResponseDto?>;
