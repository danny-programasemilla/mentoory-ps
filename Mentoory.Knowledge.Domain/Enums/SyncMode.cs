namespace Mentoory.Knowledge.Domain.Enums;

/// <summary>
/// Controls whether a project-owned knowledge structure pulls in new items from its source template.
/// Duplicated from <c>Mentoory.Diagnostic.Domain.Enums.SyncMode</c> to preserve module boundaries.
/// </summary>
public enum SyncMode
{
    Disconnected = 0,
    PartialSync = 1,
}
