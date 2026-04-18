using Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mentoory.Knowledge.Infrastructure.Persistence.Configurations;

internal sealed class TopicConfiguration : IEntityTypeConfiguration<Topic>
{
    public void Configure(EntityTypeBuilder<Topic> entity)
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
            .FindNavigation(nameof(Topic.Subjects))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
