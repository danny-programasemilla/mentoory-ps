using MediatR;
using Mentoory.Shared.Application.Interfaces;
using Mentoory.Shared.Infrastructure.Persistence;
using Mentoory.Tenant.Domain.Aggregates.Incubator;
using Mentoory.Tenant.Domain.Aggregates.Project;
using Mentoory.Tenant.Domain.Aggregates.ProjectInvitation;
using Mentoory.Tenant.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Mentoory.Tenant.Infrastructure.Persistence;

public class TenantDbContext : SharedAbstractDbContext
{
    private readonly ITenantContext _tenantContext;

    public TenantDbContext(DbContextOptions<TenantDbContext> options, IMediator mediator, ITenantContext tenantContext)
        : base(options, mediator)
    {
        _tenantContext = tenantContext;
    }

    public virtual DbSet<Incubator> Incubators { get; set; } = null!;

    public virtual DbSet<Project> Projects { get; set; } = null!;

    public virtual DbSet<ProjectStage> ProjectStages { get; set; } = null!;

    public virtual DbSet<ProjectParticipant> ProjectParticipants { get; set; } = null!;

    public virtual DbSet<MentorAssignment> MentorAssignments { get; set; } = null!;

    public virtual DbSet<ProjectInvitation> ProjectInvitations { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureIncubator(modelBuilder);
        ConfigureProject(modelBuilder, _tenantContext);
        ConfigureProjectStage(modelBuilder);
        ConfigureProjectParticipant(modelBuilder);
        ConfigureMentorAssignment(modelBuilder);
        ConfigureProjectInvitation(modelBuilder);
    }

    private static void ConfigureIncubator(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Incubator>(entity =>
        {
            entity.ToTable("Incubators", "tenant");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ExternalId).IsRequired();
            entity.HasIndex(e => e.ExternalId).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedAtUtc).IsRequired();
            entity.Property(e => e.UpdatedAtUtc).IsRequired();
        });
    }

    private static void ConfigureProject(ModelBuilder modelBuilder, ITenantContext tenantContext)
    {
        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("Projects", "tenant");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ExternalId).IsRequired();
            entity.HasIndex(e => e.ExternalId).IsUnique();
            entity.Property(e => e.IncubatorId).IsRequired();
            entity.HasIndex(e => e.IncubatorId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.CurrentStageType).IsRequired().HasConversion<byte>();
            entity.Property(e => e.CurrentStageState).IsRequired().HasConversion<byte>();
            entity.Property(e => e.IsPublic).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.EnrollmentVariant).IsRequired().HasConversion<byte>().HasDefaultValue(EnrollmentVariant.FullFlow);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedAtUtc).IsRequired();
            entity.Property(e => e.UpdatedAtUtc).IsRequired();
            entity.Property(e => e.RowVersion).IsRowVersion();

            entity.HasMany(e => e.Stages)
                .WithOne()
                .HasForeignKey("ProjectId")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Participants)
                .WithOne()
                .HasForeignKey("ProjectId")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.MentorAssignments)
                .WithOne()
                .HasForeignKey("ProjectId")
                .OnDelete(DeleteBehavior.Cascade);

            // Multi-tenant query filter
            entity.HasQueryFilter(p => tenantContext.CurrentIncubatorId == null || p.IncubatorId == tenantContext.CurrentIncubatorId);
        });
    }

    private static void ConfigureProjectStage(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProjectStage>(entity =>
        {
            entity.ToTable("ProjectStages", "tenant");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StageType).IsRequired().HasConversion<byte>();
            entity.Property(e => e.State).IsRequired().HasConversion<byte>().HasDefaultValue(StageState.NotStarted);
            entity.Property<long>("ProjectId").IsRequired();
            entity.HasIndex("ProjectId", nameof(ProjectStage.StageType)).IsUnique();
        });
    }

    private static void ConfigureProjectParticipant(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProjectParticipant>(entity =>
        {
            entity.ToTable("ProjectParticipants", "tenant");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ExternalId).IsRequired();
            entity.HasIndex(e => e.ExternalId).IsUnique();
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.EnrolledAtUtc).IsRequired();
            entity.Property<long>("ProjectId").IsRequired();
        });
    }

    private static void ConfigureMentorAssignment(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MentorAssignment>(entity =>
        {
            entity.ToTable("MentorAssignments", "tenant");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ExternalId).IsRequired();
            entity.HasIndex(e => e.ExternalId).IsUnique();
            entity.Property(e => e.MentorUserId).IsRequired();
            entity.Property(e => e.EntrepreneurUserId).IsRequired();
            entity.Property(e => e.IsLeadMentor).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.AssignedAtUtc).IsRequired();
            entity.Property<long>("ProjectId").IsRequired();
        });
    }

    private static void ConfigureProjectInvitation(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProjectInvitation>(entity =>
        {
            entity.ToTable("ProjectInvitations", "tenant");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ExternalId).IsRequired();
            entity.HasIndex(e => e.ExternalId).IsUnique();
            entity.Property(e => e.ProjectId).IsRequired();
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.TokenHash).IsRequired().HasMaxLength(128);
            entity.Property(e => e.Status).IsRequired().HasConversion<byte>().HasDefaultValue(InvitationStatus.Pending);
            entity.Property(e => e.ExpiresAtUtc).IsRequired();
            entity.Property(e => e.AcceptedAtUtc);
            entity.Property(e => e.CreatedAtUtc).IsRequired();
            entity.Property(e => e.CreatedByUserId).IsRequired();
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);

            entity.HasIndex(e => new { e.UserId, e.ProjectId })
                .IsUnique()
                .HasFilter("[IsActive] = 1 AND [Status] = 0");

            entity.HasIndex(e => new { e.ProjectId, e.Status })
                .HasFilter("[IsActive] = 1");

            entity.HasIndex(e => e.UserId)
                .HasFilter("[IsActive] = 1");
        });
    }
}
