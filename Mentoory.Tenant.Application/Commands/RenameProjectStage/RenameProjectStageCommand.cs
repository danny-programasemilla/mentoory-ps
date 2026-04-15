using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Tenant.Application.Commands.RenameProjectStage;

public sealed record RenameProjectStageCommand(
    long ProjectId,
    Guid StageExternalId,
    string DisplayName) : IBaseRequest;
