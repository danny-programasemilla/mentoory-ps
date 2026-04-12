namespace Mentoory.Access.Application.Commands.BatchCreateUsers;

public sealed record BatchCreateUsersResult(
    int TotalCount,
    int CreatedCount,
    int EnrolledCount,
    int ErrorCount,
    List<BatchCreateRowResult> Rows);
