using MediatR;
using Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructureTemplate;
using Mentoory.Knowledge.Domain.Enums;
using Mentoory.Shared.Application.Interfaces;
using Mentoory.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using KS = Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure.KnowledgeStructure;
using KsModule = Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure.Module;
using KsResource = Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure.Resource;
using KsSubject = Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure.Subject;
using KsTopic = Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure.Topic;

namespace Mentoory.Knowledge.Infrastructure.Persistence;

/// <summary>
/// Database context for the Knowledge domain (schema "knowledge").
/// </summary>
public class KnowledgeDbContext : SharedAbstractDbContext
{
    private readonly ITenantContext _tenantContext;

    public KnowledgeDbContext(DbContextOptions<KnowledgeDbContext> options, IMediator mediator, ITenantContext tenantContext)
        : base(options, mediator)
    {
        _tenantContext = tenantContext;
    }

    public virtual DbSet<KnowledgeStructureTemplate> KnowledgeStructureTemplates { get; set; } = null!;

    public virtual DbSet<KS> KnowledgeStructures { get; set; } = null!;

    internal ITenantContext TenantContext => _tenantContext;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("knowledge");

        ConfigureKnowledgeStructureTemplate(modelBuilder);
        ConfigureModuleTemplate(modelBuilder);
        ConfigureTopicTemplate(modelBuilder);
        ConfigureSubjectTemplate(modelBuilder);
        ConfigureResourceTemplate(modelBuilder);

        ConfigureKnowledgeStructure(modelBuilder, _tenantContext);
        ConfigureModule(modelBuilder);
        ConfigureTopic(modelBuilder);
        ConfigureSubject(modelBuilder);
        ConfigureResource(modelBuilder);
    }

    private static void ConfigureKnowledgeStructureTemplate(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KnowledgeStructureTemplate>(entity =>
        {
            entity.ToTable("KnowledgeStructureTemplates");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ExternalId).IsRequired();
            entity.HasIndex(e => e.ExternalId).IsUnique();

            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(2000);

            entity.Property(e => e.IsArchived).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.Version).IsRequired().HasDefaultValue(1);
            entity.Property(e => e.CreatedAtUtc).IsRequired();

            entity.HasMany(e => e.Modules)
                .WithOne()
                .HasForeignKey("KnowledgeStructureTemplateId")
                .OnDelete(DeleteBehavior.Cascade);

            entity.Metadata
                .FindNavigation(nameof(KnowledgeStructureTemplate.Modules))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);
        });
    }

    private static void ConfigureModuleTemplate(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ModuleTemplate>(entity =>
        {
            entity.ToTable("ModuleTemplates");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ExternalId).IsRequired();
            entity.HasIndex(e => e.ExternalId).IsUnique();

            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.SortOrder).IsRequired();

            entity.Property<long>("KnowledgeStructureTemplateId").IsRequired();

            entity.HasMany(e => e.Topics)
                .WithOne()
                .HasForeignKey("ModuleTemplateId")
                .OnDelete(DeleteBehavior.Cascade);

            entity.Metadata
                .FindNavigation(nameof(ModuleTemplate.Topics))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);
        });
    }

    private static void ConfigureTopicTemplate(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TopicTemplate>(entity =>
        {
            entity.ToTable("TopicTemplates");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ExternalId).IsRequired();
            entity.HasIndex(e => e.ExternalId).IsUnique();

            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.SortOrder).IsRequired();

            entity.Property(e => e.HighRangeMin).HasPrecision(10, 2);
            entity.Property(e => e.HighRangeMax).HasPrecision(10, 2);
            entity.Property(e => e.MediumRangeMin).HasPrecision(10, 2);
            entity.Property(e => e.MediumRangeMax).HasPrecision(10, 2);
            entity.Property(e => e.LowRangeMin).HasPrecision(10, 2);
            entity.Property(e => e.LowRangeMax).HasPrecision(10, 2);

            entity.Ignore(e => e.HighRange);
            entity.Ignore(e => e.MediumRange);
            entity.Ignore(e => e.LowRange);

            entity.Property<long>("ModuleTemplateId").IsRequired();

            entity.HasMany(e => e.Subjects)
                .WithOne()
                .HasForeignKey("TopicTemplateId")
                .OnDelete(DeleteBehavior.Cascade);

            entity.Metadata
                .FindNavigation(nameof(TopicTemplate.Subjects))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);
        });
    }

    private static void ConfigureSubjectTemplate(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SubjectTemplate>(entity =>
        {
            entity.ToTable("SubjectTemplates");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ExternalId).IsRequired();
            entity.HasIndex(e => e.ExternalId).IsUnique();

            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.SortOrder).IsRequired();

            entity.Property<long>("TopicTemplateId").IsRequired();

            entity.HasMany(e => e.Resources)
                .WithOne()
                .HasForeignKey("SubjectTemplateId")
                .OnDelete(DeleteBehavior.Cascade);

            entity.Metadata
                .FindNavigation(nameof(SubjectTemplate.Resources))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);
        });
    }

    private static void ConfigureResourceTemplate(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ResourceTemplate>(entity =>
        {
            entity.ToTable("ResourceTemplates");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ExternalId).IsRequired();
            entity.HasIndex(e => e.ExternalId).IsUnique();

            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Url).IsRequired().HasMaxLength(2000);

            entity.Property(e => e.ResourceType)
                .IsRequired()
                .HasConversion<byte>();

            entity.Property(e => e.SortOrder).IsRequired();

            entity.Property<long>("SubjectTemplateId").IsRequired();
        });
    }

    private static void ConfigureKnowledgeStructure(ModelBuilder modelBuilder, ITenantContext tenantContext)
    {
        modelBuilder.Entity<KS>(entity =>
        {
            entity.ToTable("KnowledgeStructures");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ExternalId).IsRequired();
            entity.HasIndex(e => e.ExternalId).IsUnique();

            entity.Property(e => e.ProjectId).IsRequired();
            entity.HasIndex(e => e.ProjectId)
                .HasDatabaseName("IX_KnowledgeStructures_ProjectId");
            entity.HasIndex(e => new { e.ProjectId, e.SourceTemplateId })
                .HasDatabaseName("IX_KnowledgeStructures_ProjectId_SourceTemplateId");

            entity.Property(e => e.IncubatorId).IsRequired();
            entity.HasIndex(e => e.IncubatorId);

            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(2000);

            entity.Property(e => e.SourceTemplateId);
            entity.Property(e => e.SourceTemplateVersion);

            entity.Property(e => e.SyncMode)
                .IsRequired()
                .HasConversion<byte>()
                .HasDefaultValue(SyncMode.Disconnected);

            entity.Property(e => e.CreatedAtUtc).IsRequired();

            entity.HasMany(e => e.Modules)
                .WithOne()
                .HasForeignKey("KnowledgeStructureId")
                .OnDelete(DeleteBehavior.Cascade);

            entity.Metadata
                .FindNavigation(nameof(KS.Modules))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);

            // Multi-tenant query filter — mirrors TenantDbContext's Projects filter (spec 017
            // US4-7 / US6-3 tenant isolation). Queries without a tenant context (e.g. seed-time
            // or integration-test scopes that never set CurrentIncubatorId) see every row; HTTP
            // requests see only rows in the caller's active incubator.
            entity.HasQueryFilter(s =>
                tenantContext.CurrentIncubatorId == null || s.IncubatorId == tenantContext.CurrentIncubatorId);
        });
    }

    private static void ConfigureModule(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KsModule>(entity =>
        {
            entity.ToTable("Modules");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ExternalId).IsRequired();
            entity.HasIndex(e => e.ExternalId).IsUnique();

            entity.Property(e => e.SourceTemplateModuleExternalId);

            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.SortOrder).IsRequired();

            entity.Property<long>("KnowledgeStructureId").IsRequired();

            entity.HasMany(e => e.Topics)
                .WithOne()
                .HasForeignKey("ModuleId")
                .OnDelete(DeleteBehavior.Cascade);

            entity.Metadata
                .FindNavigation(nameof(KsModule.Topics))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);
        });
    }

    private static void ConfigureTopic(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KsTopic>(entity =>
        {
            entity.ToTable("Topics");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ExternalId).IsRequired();
            entity.HasIndex(e => e.ExternalId).IsUnique();

            entity.Property(e => e.SourceTemplateTopicExternalId);

            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.SortOrder).IsRequired();

            entity.Property(e => e.HighRangeMin).HasPrecision(10, 2);
            entity.Property(e => e.HighRangeMax).HasPrecision(10, 2);
            entity.Property(e => e.MediumRangeMin).HasPrecision(10, 2);
            entity.Property(e => e.MediumRangeMax).HasPrecision(10, 2);
            entity.Property(e => e.LowRangeMin).HasPrecision(10, 2);
            entity.Property(e => e.LowRangeMax).HasPrecision(10, 2);

            entity.Ignore(e => e.HighRange);
            entity.Ignore(e => e.MediumRange);
            entity.Ignore(e => e.LowRange);

            entity.Property<long>("ModuleId").IsRequired();

            entity.HasMany(e => e.Subjects)
                .WithOne()
                .HasForeignKey("TopicId")
                .OnDelete(DeleteBehavior.Cascade);

            entity.Metadata
                .FindNavigation(nameof(KsTopic.Subjects))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);
        });
    }

    private static void ConfigureSubject(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KsSubject>(entity =>
        {
            entity.ToTable("Subjects");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ExternalId).IsRequired();
            entity.HasIndex(e => e.ExternalId).IsUnique();

            entity.Property(e => e.SourceTemplateSubjectExternalId);

            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.SortOrder).IsRequired();

            entity.Property<long>("TopicId").IsRequired();

            entity.HasMany(e => e.Resources)
                .WithOne()
                .HasForeignKey("SubjectId")
                .OnDelete(DeleteBehavior.Cascade);

            entity.Metadata
                .FindNavigation(nameof(KsSubject.Resources))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);
        });
    }

    private static void ConfigureResource(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KsResource>(entity =>
        {
            entity.ToTable("Resources");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ExternalId).IsRequired();
            entity.HasIndex(e => e.ExternalId).IsUnique();

            entity.Property(e => e.SourceTemplateResourceExternalId);

            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Url).IsRequired().HasMaxLength(2000);

            entity.Property(e => e.ResourceType)
                .IsRequired()
                .HasConversion<byte>();

            entity.Property(e => e.SortOrder).IsRequired();

            entity.Property<long>("SubjectId").IsRequired();
        });
    }
}
