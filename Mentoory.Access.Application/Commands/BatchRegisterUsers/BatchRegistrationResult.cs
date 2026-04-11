namespace Mentoory.Access.Application.Commands.BatchRegisterUsers;

public sealed record BatchRegistrationResult(
    int TotalRows,
    int SuccessCount,
    int SkippedCount,
    int ErrorCount,
    List<BatchRowResult> Rows);
