using System.Diagnostics.CodeAnalysis;
using Mentoory.Authorization.Infrastructure.Persistence;
using Mentoory.Diagnostic.Infrastructure.Persistence;
using Mentoory.Example.Infrastructure.Persistence;
using Mentoory.Identity.Infrastructure.Persistence;
using Mentoory.Shared.Infrastructure.Persistence;
using Mentoory.Tenant.Infrastructure.Persistence;

namespace Mentoory.Web.Infrastructure.Persistence;

public class DbContextFactory(IServiceProvider serviceProvider) : IDbContextFactory
{
    private readonly Dictionary<string, Type> _mapping = new()
    {
        { "Authorization", typeof(AuthorizationDbContext) },
        { "Diagnostic", typeof(DiagnosticDbContext) },
        { "Example", typeof(ExampleDbContext) },
        { "Identity", typeof(IdentityDbContext) },
        { "Tenant", typeof(TenantDbContext) },
    };

    public bool TryGetDbContextForRequest<TRequest>([NotNullWhen(true)] out IDbContext? dbContext)
    {
        var requestNs = typeof(TRequest).Namespace!;
        var moduleName = requestNs.Split('.')[1];

        var dbContextType = _mapping.FirstOrDefault(x => x.Key == moduleName).Value;

        if (dbContextType is null)
        {
            dbContext = null;
            return false;
        }

        dbContext = (IDbContext)serviceProvider.GetRequiredService(dbContextType);
        return true;
    }
}
