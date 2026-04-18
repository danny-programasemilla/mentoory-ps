using Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mentoory.Knowledge.Infrastructure.Persistence.Configurations;

internal sealed class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> entity)
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
            .FindNavigation(nameof(Subject.Resources))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
