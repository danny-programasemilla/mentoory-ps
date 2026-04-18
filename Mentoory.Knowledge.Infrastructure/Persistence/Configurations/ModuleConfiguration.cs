using Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mentoory.Knowledge.Infrastructure.Persistence.Configurations;

internal sealed class ModuleConfiguration : IEntityTypeConfiguration<Module>
{
    public void Configure(EntityTypeBuilder<Module> entity)
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
            .FindNavigation(nameof(Module.Topics))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
