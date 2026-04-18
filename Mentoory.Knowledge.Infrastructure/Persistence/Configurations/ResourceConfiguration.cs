using Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mentoory.Knowledge.Infrastructure.Persistence.Configurations;

internal sealed class ResourceConfiguration : IEntityTypeConfiguration<Resource>
{
    public void Configure(EntityTypeBuilder<Resource> entity)
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
    }
}
