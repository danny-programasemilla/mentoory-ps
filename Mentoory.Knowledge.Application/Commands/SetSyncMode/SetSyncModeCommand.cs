using Mentoory.Knowledge.Domain.Enums;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.SetSyncMode;

/// <summary>
/// Represents a command to change the sync mode of a knowledge structure (project clone).
/// </summary>
/// <param name="StructureExternalId">The external identifier of the knowledge structure.</param>
/// <param name="SyncMode">The desired sync mode.</param>
public sealed record SetSyncModeCommand(
    Guid StructureExternalId,
    SyncMode SyncMode) : IBaseRequest;
