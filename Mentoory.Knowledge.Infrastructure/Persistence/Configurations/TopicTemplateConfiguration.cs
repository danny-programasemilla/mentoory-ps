using Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructureTemplate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mentoory.Knowledge.Infrastructure.Persistence.Configurations;

internal sealed class TopicTemplateConfiguration : IEntityTypeConfiguration<TopicTemplate>
{
    public void Configure(EntityTypeBuilder<TopicTemplate> entity)
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
    }
}
