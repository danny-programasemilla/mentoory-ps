using Mentoory.Shared.Application.Queries.Audit;
using Microsoft.EntityFrameworkCore;

namespace Mentoory.Shared.Infrastructure.Persistence.Audit;

/// <summary>
/// Dedicated read-only <see cref="DbContext"/> over <c>[audit].[AuditLog]</c>. Change tracking is
/// disabled by default; this context MUST NOT be used to persist audit rows — writes go through
/// <see cref="Application.Audit.IAuditService"/> (raw ADO.NET) to avoid a circular DbContext dependency.
/// </summary>
public sealed class AuditReadDbContext : DbContext
{
    public AuditReadDbContext(DbContextOptions<AuditReadDbContext> options)
        : base(options)
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    public DbSet<AuditLogReadEntity> AuditLogs => Set<AuditLogReadEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<AuditLogReadEntity>();
        entity.ToTable("AuditLog", "audit");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.EventType).HasMaxLength(100);
        entity.Property(e => e.EntityType).HasMaxLength(100);
        entity.Property(e => e.EntityId).HasMaxLength(100);
        entity.Property(e => e.Action).HasMaxLength(50);
        entity.Property(e => e.IpAddress).HasMaxLength(45);
        entity.Property(e => e.Outcome).HasMaxLength(20);
        entity.Property(e => e.ExceptionType).HasMaxLength(200);
        entity.Property(e => e.UserEmail).HasMaxLength(256);
        entity.Property(e => e.RoleContext).HasMaxLength(50);
    }
}
