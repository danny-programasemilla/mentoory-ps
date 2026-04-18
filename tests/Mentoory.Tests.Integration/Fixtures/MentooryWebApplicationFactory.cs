using Mentoory.Access.Infrastructure.Persistence;
using Mentoory.Diagnostic.Infrastructure.Persistence;
using Mentoory.Example.Infrastructure.Persistence;
using Mentoory.Knowledge.Infrastructure.Persistence;
using Mentoory.Tenant.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.SqlServer.Dac;
using Respawn;
using Testcontainers.MsSql;
using Xunit;

namespace Mentoory.Tests.Integration.Fixtures;

public class MentooryWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string DatabaseName = "MentooryDb";

    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    private Respawner _respawner = null!;

    /// <summary>
    /// Connection string targeting the MentooryDb database (not master).
    /// </summary>
    public string ConnectionString
    {
        get
        {
            var builder = new SqlConnectionStringBuilder(_dbContainer.GetConnectionString())
            {
                InitialCatalog = DatabaseName,
            };
            return builder.ConnectionString;
        }
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        InitializeDatabase();

        // Set connection string as environment variable so Program.cs DI extensions
        // can find it during service registration (before ConfigureTestServices runs).
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            ConnectionString);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", null);
        await _dbContainer.StopAsync();
        await _dbContainer.DisposeAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // Replace DbContext registrations to bypass Aspire enrichment
        // (which expects orchestrator-managed configuration).
        builder.ConfigureServices(services =>
        {
            var connStr = ConnectionString;

            ReplaceDbContext<AccessDbContext>(services, connStr);
            ReplaceDbContext<TenantDbContext>(services, connStr);
            ReplaceDbContext<DiagnosticDbContext>(services, connStr);
            ReplaceDbContext<ExampleDbContext>(services, connStr);
            ReplaceDbContext<KnowledgeDbContext>(services, connStr);
        });
    }

    private static void ReplaceDbContext<TContext>(IServiceCollection services, string connectionString)
        where TContext : DbContext
    {
        services.RemoveAll<TContext>();
        services.RemoveAll<DbContextOptions<TContext>>();

        services.AddDbContext<TContext>((sp, opts) =>
        {
            opts.UseSqlServer(connectionString);
            opts.EnableSensitiveDataLogging();
            opts.EnableDetailedErrors();
        });
    }

    private static string FindDacpac()
    {
        // Walk up from the test assembly's output directory to the repo root,
        // then resolve the DACPAC from the Mentoory.Db build output.
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            var candidate = Path.Combine(dir, "Mentoory.Db", "bin", "Debug", "MentooryDb.dacpac");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new FileNotFoundException(
            "MentooryDb.dacpac not found. Build the Mentoory.Db project before running integration tests: " +
            "dotnet build Mentoory.Db/MentooryDb.sqlproj");
    }

    private void InitializeDatabase()
    {
        // Deploy the real DACPAC so integration tests run against the same schema
        // as production. This catches EF-model ↔ DB-schema drift that
        // EnsureCreatedAsync() would silently hide.
        var dacpacPath = FindDacpac();
        var masterConnStr = _dbContainer.GetConnectionString();
        var dacServices = new DacServices(masterConnStr);
        using var package = DacPackage.Load(dacpacPath);

        dacServices.Deploy(package, DatabaseName, upgradeExisting: true, options: new DacDeployOptions
        {
            CreateNewDatabase = true,
            BlockOnPossibleDataLoss = false,
            IncludeTransactionalScripts = false,
        });

        // Initialize Respawn against the deployed database
        using var connection = new SqlConnection(ConnectionString);
        connection.Open();
        _respawner = Respawner.CreateAsync(connection, new RespawnerOptions
        {
            SchemasToInclude = ["access", "tenant", "diagnostic", "example", "knowledge", "subscription"],
            DbAdapter = DbAdapter.SqlServer,
        }).GetAwaiter().GetResult();
    }
}
