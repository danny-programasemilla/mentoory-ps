using Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructureTemplate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mentoory.Knowledge.Infrastructure.Persistence.Configurations;

internal sealed class ResourceTemplateConfiguration : IEntityTypeConfiguration<ResourceTemplate>
{
    public void Configure(EntityTypeBuilder<ResourceTemplate> entity)
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
    }
}
