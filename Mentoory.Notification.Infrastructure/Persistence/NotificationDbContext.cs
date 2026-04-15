using MediatR;
using Mentoory.Notification.Domain.Aggregates.Notification;
using Mentoory.Notification.Domain.Enums;
using Mentoory.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NotificationAggregate = Mentoory.Notification.Domain.Aggregates.Notification.Notification;
using NotificationConfigurationAggregate = Mentoory.Notification.Domain.Aggregates.NotificationConfiguration.NotificationConfiguration;
using NotificationPreferenceAggregate = Mentoory.Notification.Domain.Aggregates.NotificationPreference.NotificationPreference;

namespace Mentoory.Notification.Infrastructure.Persistence;

public class NotificationDbContext : SharedAbstractDbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options, IMediator mediator)
        : base(options, mediator)
    {
    }

    public virtual DbSet<NotificationAggregate> Notifications { get; set; } = null!;

    public virtual DbSet<NotificationPreferenceAggregate> NotificationPreferences { get; set; } = null!;

    public virtual DbSet<NotificationConfigurationAggregate> NotificationConfigurations { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureNotification(modelBuilder);
        ConfigureNotificationRecipient(modelBuilder);
        ConfigureDeliveryAttempt(modelBuilder);
        ConfigureNotificationPreference(modelBuilder);
        ConfigureNotificationConfiguration(modelBuilder);
    }

    private static void ConfigureNotification(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationAggregate>(entity =>
        {
            entity.ToTable("Notifications", "notification");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.ExternalId)
                .IsRequired();

            entity.HasIndex(e => e.ExternalId)
                .IsUnique();

            entity.Property(e => e.NotificationType)
                .IsRequired()
                .HasConversion<byte>();

            entity.Property(e => e.Subject)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.HtmlBody)
                .IsRequired();

            entity.Property(e => e.SourceEventId);

            entity.HasIndex(e => new { e.SourceEventId, e.NotificationType })
                .IsUnique()
                .HasFilter("[SourceEventId] IS NOT NULL");

            entity.HasIndex(e => e.ScheduledForUtc);

            entity.Property(e => e.ScheduledForUtc)
                .IsRequired();

            entity.Property(e => e.CreatedAtUtc)
                .IsRequired();

            entity.HasMany(e => e.Recipients)
                .WithOne()
                .HasForeignKey("NotificationId")
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureNotificationRecipient(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationRecipient>(entity =>
        {
            entity.ToTable("NotificationRecipients", "notification");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.UserId)
                .IsRequired();

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.DeliveryChannel)
                .IsRequired()
                .HasConversion<byte>();

            entity.Property(e => e.DeliveryStatus)
                .IsRequired()
                .HasConversion<byte>();

            entity.Property(e => e.SentAtUtc);

            entity.Property(e => e.FailureReason)
                .HasMaxLength(1024);

            entity.HasIndex(e => e.DeliveryStatus)
                .HasFilter("[DeliveryStatus] = 0");

            entity.Property<long>("NotificationId")
                .IsRequired();

            entity.HasMany(e => e.DeliveryAttempts)
                .WithOne()
                .HasForeignKey("NotificationRecipientId")
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureDeliveryAttempt(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DeliveryAttempt>(entity =>
        {
            entity.ToTable("DeliveryAttempts", "notification");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.AttemptNumber)
                .IsRequired();

            entity.Property(e => e.AttemptedAtUtc)
                .IsRequired();

            entity.Property(e => e.Success)
                .IsRequired();

            entity.Property(e => e.FailureReason)
                .HasMaxLength(1024);

            entity.Property<long>("NotificationRecipientId")
                .IsRequired();
        });
    }

    private static void ConfigureNotificationPreference(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationPreferenceAggregate>(entity =>
        {
            entity.ToTable("NotificationPreferences", "notification");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.ExternalId)
                .IsRequired();

            entity.HasIndex(e => e.ExternalId)
                .IsUnique();

            entity.Property(e => e.UserId)
                .IsRequired();

            entity.Property(e => e.NotificationType)
                .IsRequired()
                .HasConversion<byte>();

            entity.HasIndex(e => new { e.UserId, e.NotificationType })
                .IsUnique();

            entity.Property(e => e.IsEnabled)
                .IsRequired();

            entity.Property(e => e.UpdatedAtUtc)
                .IsRequired();
        });
    }

    private static void ConfigureNotificationConfiguration(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationConfigurationAggregate>(entity =>
        {
            entity.ToTable("NotificationConfigurations", "notification");
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
}
