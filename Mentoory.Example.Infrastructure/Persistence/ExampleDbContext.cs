using MediatR;
using Mentoory.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mentoory.Example.Infrastructure.Persistence;

/// <summary>
/// Database context for the Example domain, providing access to example entities.
/// </summary>
/// <param name="options">The options to configure the database context.</param>
/// <param name="mediator">The MediatR mediator for dispatching domain events.</param>
public class ExampleDbContext(DbContextOptions<ExampleDbContext> options, IMediator mediator) : SharedAbstractDbContext(options, mediator)
{
    /// <summary>
    /// Gets or sets the DbSet for Example entities.
    /// </summary>
    public virtual DbSet<Domain.Aggregates.Example.Example> Examples { get; set; }

    /// <summary>
    /// Configures the entity mappings and database schema for the Example domain.
    /// </summary>
    /// <param name="modelBuilder">The builder used to construct the model for this context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Domain.Aggregates.Example.Example>(entity =>
        {
            entity.ToTable("Examples", "example");

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);
        });
    }
}
