using MediatR;
using Mentoory.Access.Domain.Aggregates.AuthSession;
using Mentoory.Access.Domain.Aggregates.Country;
using Mentoory.Access.Domain.Aggregates.RoleAssignment;
using Mentoory.Access.Domain.Aggregates.SystemConfiguration;
using Mentoory.Access.Domain.Aggregates.User;
using Mentoory.Access.Domain.Enums;
using Mentoory.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mentoory.Access.Infrastructure.Persistence;

/// <summary>
/// Database context for the Access domain, providing access to identity and authorization entities.
/// </summary>
public class AccessDbContext : SharedAbstractDbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AccessDbContext"/> class.
    /// </summary>
    /// <param name="options">The options to configure the database context.</param>
    /// <param name="mediator">The MediatR mediator for dispatching domain events.</param>
    public AccessDbContext(DbContextOptions<AccessDbContext> options, IMediator mediator)
        : base(options, mediator)
    {
    }

    /// <summary>
    /// Gets or sets the DbSet for User entities.
    /// </summary>
    public virtual DbSet<User> Users { get; set; } = null!;

    /// <summary>
    /// Gets or sets the DbSet for Credential entities.
    /// </summary>
    public virtual DbSet<Credential> Credentials { get; set; } = null!;

    /// <summary>
    /// Gets or sets the DbSet for AuthSession entities.
    /// </summary>
    public virtual DbSet<AuthSession> AuthSessions { get; set; } = null!;

    /// <summary>
    /// Gets or sets the DbSet for EmailVerificationToken entities.
    /// </summary>
    public virtual DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; } = null!;

    /// <summary>
    /// Gets or sets the DbSet for PasswordResetToken entities.
    /// </summary>
    public virtual DbSet<PasswordResetToken> PasswordResetTokens { get; set; } = null!;

    /// <summary>
    /// Gets or sets the DbSet for RoleAssignment entities.
    /// </summary>
    public virtual DbSet<RoleAssignment> RoleAssignments { get; set; } = null!;

    public virtual DbSet<SystemConfiguration> SystemConfigurations { get; set; } = null!;

    public virtual DbSet<Country> Countries { get; set; } = null!;

    /// <summary>
    /// Configures the entity mappings and database schema for the Access domain.
    /// </summary>
    /// <param name="modelBuilder">The builder used to construct the model for this context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureUser(modelBuilder);
        ConfigureCredential(modelBuilder);
        ConfigureAuthSession(modelBuilder);
        ConfigureEmailVerificationToken(modelBuilder);
        ConfigurePasswordResetToken(modelBuilder);
        ConfigureRoleAssignment(modelBuilder);
        ConfigureSystemConfiguration(modelBuilder);
        ConfigureCountry(modelBuilder);
    }

    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users", "access");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.ExternalId)
                .IsRequired();

            entity.HasIndex(e => e.ExternalId)
                .IsUnique();

            // Owned value object: EmailAddress
            entity.OwnsOne(e => e.Email, email =>
            {
                email.Property(v => v.Value)
                    .HasColumnName("Email")
                    .HasMaxLength(256)
                    .IsRequired();

                email.Property(v => v.NormalizedValue)
                    .HasColumnName("NormalizedEmail")
                    .HasMaxLength(256)
                    .IsRequired();

                email.HasIndex(v => v.NormalizedValue)
                    .IsUnique();
            });

            // Owned value object: NationalIdentity
            entity.OwnsOne(e => e.NationalIdentity, ni =>
            {
                ni.Property(v => v.Country)
                    .HasColumnName("Country")
                    .HasMaxLength(3)
                    .IsRequired();

                ni.Property(v => v.NationalId)
                    .HasColumnName("NationalId")
                    .HasMaxLength(50)
                    .IsRequired();

                ni.HasIndex(v => new { v.Country, v.NationalId })
                    .IsUnique();
            });

            entity.Property(e => e.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.LastName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.AccountStatus)
                .IsRequired()
                .HasConversion<byte>()
                .HasDefaultValue(AccountStatus.PendingVerification);

            entity.Property(e => e.FailedLoginAttempts)
                .IsRequired()
                .HasDefaultValue(0);

            entity.Property(e => e.LockoutEndUtc);

            entity.Property(e => e.EmailVerifiedAtUtc);

            entity.Property(e => e.CreatedAtUtc)
                .IsRequired();

            entity.Property(e => e.UpdatedAtUtc)
                .IsRequired();

            // Navigation: Credentials
            entity.HasMany(e => e.Credentials)
                .WithOne()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade);

            // Navigation: EmailVerificationTokens
            entity.HasMany(e => e.EmailVerificationTokens)
                .WithOne()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade);

            // Navigation: PasswordResetTokens
            entity.HasMany(e => e.PasswordResetTokens)
                .WithOne()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureCredential(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Credential>(entity =>
        {
            entity.ToTable("Credentials", "access");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.PasswordHash)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.IsActive)
                .IsRequired();

            entity.Property(e => e.CreatedAtUtc)
                .IsRequired();

            entity.Property<long>("UserId")
                .IsRequired();
        });
    }

    private static void ConfigureAuthSession(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuthSession>(entity =>
        {
            entity.ToTable("AuthSessions", "access");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.SessionToken)
                .IsRequired()
                .HasMaxLength(500);

            entity.HasIndex(e => e.SessionToken)
                .IsUnique();

            entity.Property(e => e.UserId)
                .IsRequired();

            entity.Property(e => e.IpAddress)
                .IsRequired()
                .HasMaxLength(45);

            entity.Property(e => e.UserAgent)
                .HasMaxLength(500);

            entity.Property(e => e.CreatedAtUtc)
                .IsRequired();

            entity.Property(e => e.LastActivityUtc)
                .IsRequired();

            entity.Property(e => e.ExpiresAtUtc)
                .IsRequired();

            entity.Property(e => e.IsActive)
                .IsRequired();

            entity.Property(e => e.ActiveIncubatorId);

            entity.Property(e => e.ActiveProjectId);

            entity.Property(e => e.ActiveRole)
                .HasMaxLength(100);

            entity.HasIndex(e => new { e.UserId, e.IsActive });
        });
    }

    private static void ConfigureEmailVerificationToken(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmailVerificationToken>(entity =>
        {
            entity.ToTable("EmailVerificationTokens", "access");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.TokenHash)
                .IsRequired()
                .HasMaxLength(500);

            entity.HasIndex(e => e.TokenHash);

            entity.Property(e => e.ExpiresAtUtc)
                .IsRequired();

            entity.Property(e => e.IsUsed)
                .IsRequired();

            entity.Property(e => e.CreatedAtUtc)
                .IsRequired();

            entity.Property<long>("UserId")
                .IsRequired();
        });
    }

    private static void ConfigurePasswordResetToken(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.ToTable("PasswordResetTokens", "access");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.TokenHash)
                .IsRequired()
                .HasMaxLength(500);

            entity.HasIndex(e => e.TokenHash);

            entity.Property(e => e.ExpiresAtUtc)
                .IsRequired();

            entity.Property(e => e.IsUsed)
                .IsRequired();

            entity.Property(e => e.CreatedAtUtc)
                .IsRequired();

            entity.Property<long>("UserId")
                .IsRequired();
        });
    }

    private static void ConfigureRoleAssignment(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RoleAssignment>(entity =>
        {
            entity.ToTable("RoleAssignments", "access");

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

    private static void ConfigureSystemConfiguration(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SystemConfiguration>(entity =>
        {
            entity.ToTable("SystemConfigurations", "access");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ExternalId).IsRequired();
            entity.HasIndex(e => e.ExternalId).IsUnique();
            entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Key).IsUnique();
            entity.Property(e => e.Value).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.DataType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CreatedAtUtc).IsRequired();
            entity.Property(e => e.UpdatedAtUtc).IsRequired();
        });
    }

    private static void ConfigureCountry(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Country>(entity =>
        {
            entity.ToTable("Countries", "access");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ExternalId).IsRequired();
            entity.HasIndex(e => e.ExternalId).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(3);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.IdentificationLabel).IsRequired().HasMaxLength(100);
            entity.Property(e => e.IdentificationMask).HasMaxLength(50);
            entity.Property(e => e.IdentificationRegex).HasMaxLength(200);
            entity.Property(e => e.IdentificationMaxLength).IsRequired();
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedAtUtc).IsRequired();
        });
    }
}
