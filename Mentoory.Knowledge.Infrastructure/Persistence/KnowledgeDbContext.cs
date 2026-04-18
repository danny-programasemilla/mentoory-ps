using MediatR;
using Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructureTemplate;
using Mentoory.Shared.Application.Interfaces;
using Mentoory.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mentoory.Knowledge.Infrastructure.Persistence;

/// <summary>
/// Database context for the Knowledge domain (schema "knowledge").
/// </summary>
public class KnowledgeDbContext : SharedAbstractDbContext
{
    private readonly ITenantContext _tenantContext;

    public KnowledgeDbContext(DbContextOptions<KnowledgeDbContext> options, IMediator mediator, ITenantContext tenantContext)
        : base(options, mediator)
    {
        _tenantContext = tenantContext;
    }

    public virtual DbSet<KnowledgeStructureTemplate> KnowledgeStructureTemplates { get; set; } = null!;

    public virtual DbSet<Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure.KnowledgeStructure> KnowledgeStructures { get; set; } = null!;

    internal ITenantContext TenantContext => _tenantContext;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("knowledge");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KnowledgeDbContext).Assembly);
    }
}
