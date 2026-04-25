using Mentoory.Shared.Application.Audit;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Mentoory.Shared.Infrastructure.Audit;

/// <summary>
/// Writes audit log entries directly to the [audit].[AuditLog] table via ADO.NET
/// to avoid circular DbContext dependencies. Best-effort: failures are logged, not thrown.
/// </summary>
public class AuditService : IAuditService
{
    private const string InsertSql = """
        INSERT INTO [audit].[AuditLog]
            ([EventType], [UserId], [IncubatorId], [ProjectId], [EntityType], [EntityId],
             [Action], [Details], [IpAddress], [OccurredAtUtc],
             [CorrelationId], [Outcome], [ExceptionType], [UserEmail], [RoleContext])
        VALUES
            (@EventType, @UserId, @IncubatorId, @ProjectId, @EntityType, @EntityId,
             @Action, @Details, @IpAddress, @OccurredAtUtc,
             @CorrelationId, @Outcome, @ExceptionType, @UserEmail, @RoleContext)
        """;

    private readonly string _connectionString;
    private readonly ILogger<AuditService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditService"/> class.
    /// </summary>
    /// <param name="configuration">Application configuration for connection string lookup.</param>
    /// <param name="logger">Logger instance for error reporting.</param>
    public AuditService(IConfiguration configuration, ILogger<AuditService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new SqlCommand(InsertSql, connection);
            command.Parameters.AddWithValue("@EventType", entry.EventType);
            command.Parameters.AddWithValue("@UserId", (object?)entry.UserId ?? DBNull.Value);
            command.Parameters.AddWithValue("@IncubatorId", (object?)entry.IncubatorId ?? DBNull.Value);
            command.Parameters.AddWithValue("@ProjectId", (object?)entry.ProjectId ?? DBNull.Value);
            command.Parameters.AddWithValue("@EntityType", (object?)entry.EntityType ?? DBNull.Value);
            command.Parameters.AddWithValue("@EntityId", (object?)entry.EntityId ?? DBNull.Value);
            command.Parameters.AddWithValue("@Action", entry.Action);
            command.Parameters.AddWithValue("@Details", (object?)entry.Details ?? DBNull.Value);
            command.Parameters.AddWithValue("@IpAddress", (object?)entry.IpAddress ?? DBNull.Value);
            command.Parameters.AddWithValue("@OccurredAtUtc", entry.OccurredAtUtc);
            command.Parameters.AddWithValue("@CorrelationId", (object?)entry.CorrelationId ?? DBNull.Value);
            command.Parameters.AddWithValue("@Outcome", entry.Outcome);
            command.Parameters.AddWithValue("@ExceptionType", (object?)entry.ExceptionType ?? DBNull.Value);
            command.Parameters.AddWithValue("@UserEmail", (object?)entry.UserEmail ?? DBNull.Value);
            command.Parameters.AddWithValue("@RoleContext", (object?)entry.RoleContext ?? DBNull.Value);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log entry: {EventType}", entry.EventType);
        }
    }
}
