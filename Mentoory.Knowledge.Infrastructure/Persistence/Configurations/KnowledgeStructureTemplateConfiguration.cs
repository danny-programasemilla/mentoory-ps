using Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructureTemplate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mentoory.Knowledge.Infrastructure.Persistence.Configurations;

internal sealed class KnowledgeStructureTemplateConfiguration : IEntityTypeConfiguration<KnowledgeStructureTemplate>
{
    public void Configure(EntityTypeBuilder<KnowledgeStructureTemplate> entity)
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
    }
}
