using Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructureTemplate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mentoory.Knowledge.Infrastructure.Persistence.Configurations;

internal sealed class SubjectTemplateConfiguration : IEntityTypeConfiguration<SubjectTemplate>
{
    public void Configure(EntityTypeBuilder<SubjectTemplate> entity)
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
    }
}
