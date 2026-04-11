using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Tenant.Application.Commands.CreateIncubator;

public sealed record CreateIncubatorCommand(string Name, string? Description) : IBaseRequest<Guid>;
