using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Tenant.Application.Queries.ResolveProjectExternalId;

/// <summary>
/// Resolves a project's external identifier from its internal identifier.
/// </summary>
public sealed record ResolveProjectExternalIdQuery(long ProjectId) : IBaseRequest<Guid>;
