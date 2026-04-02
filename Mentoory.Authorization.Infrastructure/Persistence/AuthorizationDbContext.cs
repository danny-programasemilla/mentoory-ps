using MediatR;
using Mentoory.Authorization.Domain.Aggregates.RoleAssignment;
using Mentoory.Authorization.Domain.ReadModels;
using Mentoory.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mentoory.Authorization.Infrastructure.Persistence;

/// <summary>
/// Database context for the Authorization domain, providing access to role assignment entities.
/// </summary>
/// <param name="options">The options to configure the database context.</param>
/// <param name="mediator">The MediatR mediator for dispatching domain events.</param>
public class AuthorizationDbContext(
    DbContextOptions<AuthorizationDbContext> options,
    IMediator mediator)
    : SharedAbstractDbContext(options, mediator)
{
    /// <summary>
    /// Gets or sets the DbSet for RoleAssignment entities.
    /// </summary>
    public virtual DbSet<RoleAssignment> RoleAssignments { get; set; }

    /// <summary>
    /// Gets or sets the DbSet for UserProfile read model entities.
    /// </summary>
    public virtual DbSet<UserProfile> UserProfiles { get; set; }

    /// <summary>
    /// Configures the entity mappings and database schema for the Authorization domain.
    /// </summary>
    /// <param name="modelBuilder">The builder used to construct the model for this context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureUserProfile(modelBuilder);

        modelBuilder.Entity<RoleAssignment>(entity =>
        {
            entity.ToTable("RoleAssignments", "authorization");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("Id");

            entity.Property(e => e.ExternalId)
                .IsRequired();

            entity.HasIndex(e => e.ExternalId)
                .IsUnique();

            entity.Property(e => e.UserId)
                .IsRequired();

            entity.HasIndex(e => e.UserId);

            entity.Property(e => e.IncubatorId)
                .IsRequired();

            entity.Property(e => e.ProjectId);

            entity.Property(e => e.Role)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.IsActive)
                .IsRequired();

            entity.Property(e => e.CreatedAtUtc)
                .IsRequired();

            entity.Property(e => e.UpdatedAtUtc)
                .IsRequired();

            // Composite index for efficient lookups
            entity.HasIndex(e => new { e.UserId, e.IncubatorId, e.Role, e.IsActive });
        });
    }

    private static void ConfigureUserProfile(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.ToTable("UserProfiles", "authorization");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.UserId)
                .IsRequired();

            entity.HasIndex(e => e.UserId)
                .IsUnique();

            entity.Property(e => e.UserExternalId)
                .IsRequired();

            entity.HasIndex(e => e.UserExternalId)
                .IsUnique();

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.LastName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.AccountStatus)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.CreatedAtUtc)
                .IsRequired();

            entity.Property(e => e.LastSyncedAtUtc)
                .IsRequired();
        });
    }
}
