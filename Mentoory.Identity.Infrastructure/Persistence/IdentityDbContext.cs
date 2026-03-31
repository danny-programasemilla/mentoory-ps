using MediatR;
using Mentoory.Identity.Domain.Aggregates.AuthSession;
using Mentoory.Identity.Domain.Aggregates.User;
using Mentoory.Identity.Domain.Enums;
using Mentoory.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mentoory.Identity.Infrastructure.Persistence;

/// <summary>
/// Database context for the Identity domain, providing access to identity entities.
/// </summary>
public class IdentityDbContext : SharedAbstractDbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IdentityDbContext"/> class.
    /// </summary>
    /// <param name="options">The options to configure the database context.</param>
    /// <param name="mediator">The MediatR mediator for dispatching domain events.</param>
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options, IMediator mediator)
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
    /// Configures the entity mappings and database schema for the Identity domain.
    /// </summary>
    /// <param name="modelBuilder">The builder used to construct the model for this context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureUser(modelBuilder);
        ConfigureCredential(modelBuilder);
        ConfigureAuthSession(modelBuilder);
        ConfigureEmailVerificationToken(modelBuilder);
        ConfigurePasswordResetToken(modelBuilder);
    }

    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users", "identity");

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
            entity.ToTable("Credentials", "identity");

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
            entity.ToTable("AuthSessions", "identity");

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
            entity.ToTable("EmailVerificationTokens", "identity");

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
            entity.ToTable("PasswordResetTokens", "identity");

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
}
