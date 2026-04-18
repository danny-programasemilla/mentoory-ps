using Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructureTemplate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mentoory.Knowledge.Infrastructure.Persistence.Configurations;

internal sealed class ModuleTemplateConfiguration : IEntityTypeConfiguration<ModuleTemplate>
{
    public void Configure(EntityTypeBuilder<ModuleTemplate> entity)
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
    }
}
