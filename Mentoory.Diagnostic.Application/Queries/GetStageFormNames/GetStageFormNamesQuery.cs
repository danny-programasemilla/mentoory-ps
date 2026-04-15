using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Diagnostic.Application.Queries.GetStageFormNames;

public sealed record GetStageFormNamesQuery(long ProjectId) : IBaseRequest<StageFormNamesDto>;
