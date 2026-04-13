using Mentoory.Access.Application.Commands.CreateUser;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Mentoory.Access.Application.Commands.BatchCreateUsers;

public partial class BatchCreateUsersCommandHandler
    : BaseCommandHandler<BatchCreateUsersCommand, BatchCreateUsersResult>
{
    private readonly IMediator _mediator;
    private readonly ILogger<BatchCreateUsersCommandHandler> _logger;

    public BatchCreateUsersCommandHandler(
        IMediator mediator,
        ILogger<BatchCreateUsersCommandHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public override async Task<Result<BatchCreateUsersResult>> Handle(
        BatchCreateUsersCommand request,
        CancellationToken cancellationToken)
    {
        var rows = new List<BatchCreateRowResult>();
        var createdCount = 0;
        var enrolledCount = 0;
        var errorCount = 0;

        for (var i = 0; i < request.Rows.Count; i++)
        {
            var record = request.Rows[i];
            var rowNumber = i + 1;

            try
            {
                var command = new CreateUserCommand(
                    record.Email,
                    record.Country,
                    record.Identification,
                    record.FirstName,
                    record.LastName,
                    request.SkipEmailVerification,
                    request.SkipInvitationAcceptance,
                    request.ProjectExternalId,
                    request.CreatedByUserId);

                var result = await _mediator.Send(command, cancellationToken);

                if (result.IsSuccess)
                {
                    var value = result.Value!;
                    switch (value.Outcome)
                    {
                        case CreateUserOutcome.Created: createdCount++; break;
                        case CreateUserOutcome.Enrolled: enrolledCount++; break;
                        case CreateUserOutcome.AlreadyEnrolled: enrolledCount++; break;
                    }

                    rows.Add(new BatchCreateRowResult
                    {
                        RowNumber = rowNumber,
                        Country = record.Country,
                        Identification = record.Identification,
                        Email = record.Email,
                        Outcome = value.Outcome,
                        TemporaryPassword = value.TemporaryPassword,
                        Warnings = [.. value.Warnings],
                    });
                }
                else
                {
                    errorCount++;
                    rows.Add(new BatchCreateRowResult
                    {
                        RowNumber = rowNumber,
                        Country = record.Country,
                        Identification = record.Identification,
                        Email = record.Email,
                        Errors = result.ErrorMessages?.Select(e => e.Message).ToList() ?? [],
                    });
                }
            }
            catch (Exception ex)
            {
                errorCount++;
                rows.Add(new BatchCreateRowResult
                {
                    RowNumber = rowNumber,
                    Country = record.Country,
                    Identification = record.Identification,
                    Email = record.Email,
                    Errors = [ex.Message],
                });
                LogRowError(rowNumber, ex.Message);
            }
        }

        LogBatchCompleted(request.Rows.Count, createdCount, enrolledCount, errorCount);

        return Success(new BatchCreateUsersResult(
            request.Rows.Count, createdCount, enrolledCount, errorCount, rows));
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Batch create row {RowNumber} error: {ErrorMessage}")]
    partial void LogRowError(int rowNumber, string errorMessage);

    [LoggerMessage(Level = LogLevel.Information, Message = "Batch create completed: {Total} rows, {Created} created, {Enrolled} enrolled, {Errors} errors")]
    partial void LogBatchCompleted(int total, int created, int enrolled, int errors);
}
