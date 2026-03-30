using System.Diagnostics.CodeAnalysis;
using Mentoory.Example.Infrastructure.Persistence;
using Mentoory.Shared.Infrastructure.Persistence;

namespace Mentoory.Web.Infrastructure.Persistence;

public class DbContextFactory(IServiceProvider serviceProvider) : IDbContextFactory
{
    private readonly Dictionary<string, Type> _mapping = new() { { "Example", typeof(ExampleDbContext) }, };

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
