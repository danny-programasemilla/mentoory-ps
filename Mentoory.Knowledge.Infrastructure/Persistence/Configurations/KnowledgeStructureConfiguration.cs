using Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure;
using Mentoory.Knowledge.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mentoory.Knowledge.Infrastructure.Persistence.Configurations;

internal sealed class KnowledgeStructureConfiguration : IEntityTypeConfiguration<KnowledgeStructure>
{
    public void Configure(EntityTypeBuilder<KnowledgeStructure> entity)
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
            .FindNavigation(nameof(KnowledgeStructure.Modules))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
