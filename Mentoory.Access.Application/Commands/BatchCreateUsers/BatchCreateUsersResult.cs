namespace Mentoory.Access.Application.Commands.BatchCreateUsers;

public sealed record BatchCreateUsersResult(
    int TotalCount,
    int CreatedCount,
    int EnrolledCount,
    int ErrorCount,
    IReadOnlyList<BatchCreateRowResult> Rows);
