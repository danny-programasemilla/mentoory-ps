using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Tenant.Application.Commands.UpdateIncubator;

public sealed record UpdateIncubatorCommand(Guid ExternalId, string Name, string? Description) : IBaseRequest;
