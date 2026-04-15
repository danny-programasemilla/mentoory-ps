namespace Mentoory.Diagnostic.Application.Queries.GetStageFormNames;

public sealed record StageFormNamesDto(
    IReadOnlyDictionary<long, IReadOnlyList<string>> FormNamesByStageId);
