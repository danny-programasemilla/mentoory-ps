using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Http;
using Mentoory.Authorization.Infrastructure.Persistence;
using Mentoory.Diagnostic.Infrastructure.Persistence;
using Mentoory.Example.Infrastructure.Persistence;
using Mentoory.Identity.Application.Queries.ListUsers.Abstractions;
using Mentoory.Identity.Domain.Aggregates.User;
using Mentoory.Identity.Infrastructure.Persistence;
using Mentoory.Tenant.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Playwright;
using Microsoft.SqlServer.Dac;
using Testcontainers.MsSql;
using Xunit;

namespace Mentoory.Tests.E2E.Infrastructure;

public class PlaywrightFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string DatabaseName = "MentooryDb";

    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    private IHost? _kestrelHost;

    public string BaseUrl { get; private set; } = string.Empty;
    public IPlaywright? Playwright { get; private set; }
    public IBrowser? Browser { get; private set; }

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

    public async Task<IBrowserContext> CreateBrowserContextAsync()
    {
        return await Browser!.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true,
            ScreenSize = new ScreenSize { Width = 1280, Height = 720 }
        });
    }

    public async Task<IPage> CreatePageAsync()
    {
        var context = await CreateBrowserContextAsync();
        return await context.NewPageAsync();
    }

    public async Task TakeScreenshotOnFailureAsync(IPage page, string testName)
    {
        var screenshotDir = Path.Combine(AppContext.BaseDirectory, "screenshots");
        Directory.CreateDirectory(screenshotDir);

        var filePath = Path.Combine(screenshotDir,
            $"{testName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png");

        await page.ScreenshotAsync(new PageScreenshotOptions { Path = filePath, FullPage = true });
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        // 1. Start SQL Server container
        await _dbContainer.StartAsync();

        // 2. Deploy schema via DACPAC (includes PostDeployment seed data)
        InitializeDatabase();

        // 3. Set connection string for Program.cs
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            ConnectionString);

        // 4. Create TestServer host (WAF default) — needed for DI/services access
        //    This also triggers ConfigureWebHost overrides.
        _ = CreateDefaultClient();

        // 5. Start a real Kestrel host that proxies through the TestServer handler
        var testServer = Server;
        var handler = testServer.CreateHandler();
        var port = GetRandomPort();
        BaseUrl = $"http://127.0.0.1:{port}";

        _kestrelHost = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(wb =>
            {
                wb.UseKestrel(opts => opts.ListenLocalhost(port));
                wb.Configure(app =>
                {
                    app.Use(next => ctx => ProxyRequestAsync(ctx, handler));
                });
            })
            .Build();

        await _kestrelHost.StartAsync();

        // 6. Launch Playwright browser
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        if (Browser is not null)
        {
            await Browser.DisposeAsync();
        }

        Playwright?.Dispose();

        if (_kestrelHost is not null)
        {
            await _kestrelHost.StopAsync();
            _kestrelHost.Dispose();
        }

        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", null);
        await _dbContainer.StopAsync();
        await _dbContainer.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            var connStr = ConnectionString;

            ReplaceDbContext<IdentityDbContext>(services, connStr);
            ReplaceDbContext<AuthorizationDbContext>(services, connStr);
            ReplaceDbContext<TenantDbContext>(services, connStr);
            ReplaceDbContext<DiagnosticDbContext>(services, connStr);
            ReplaceDbContext<ExampleDbContext>(services, connStr);

            services.AddScoped<IIdentityQueryContext>(sp =>
            {
                var dbContext = sp.GetRequiredService<IdentityDbContext>();
                var authDbContext = sp.GetRequiredService<AuthorizationDbContext>();
                return new IdentityQueryContextAdapter(dbContext, authDbContext);
            });
        });
    }

    private static int GetRandomPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static void ReplaceDbContext<TContext>(IServiceCollection services, string connectionString)
        where TContext : DbContext
    {
        services.RemoveAll<TContext>();
        services.RemoveAll<DbContextOptions<TContext>>();

        services.AddDbContext<TContext>((_, opts) =>
        {
            opts.UseSqlServer(connectionString);
            opts.EnableSensitiveDataLogging();
            opts.EnableDetailedErrors();
        });
    }

    private static string FindDacpac()
    {
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
            "MentooryDb.dacpac not found. Build the Mentoory.Db project before running E2E tests: " +
            "dotnet build Mentoory.Db/MentooryDb.sqlproj");
    }

    private static async Task ProxyRequestAsync(HttpContext context, HttpMessageHandler handler)
    {
        var invoker = new HttpMessageInvoker(handler);

        var targetUri = new Uri($"http://localhost{context.Request.Path}{context.Request.QueryString}");
        var requestMessage = new HttpRequestMessage(new HttpMethod(context.Request.Method), targetUri);

        foreach (var header in context.Request.Headers)
        {
            requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }

        if (context.Request.ContentLength > 0 || context.Request.ContentType is not null)
        {
            requestMessage.Content = new StreamContent(context.Request.Body);
            if (context.Request.ContentType is not null)
            {
                requestMessage.Content.Headers.TryAddWithoutValidation("Content-Type", context.Request.ContentType);
            }
        }

        var response = await invoker.SendAsync(requestMessage, context.RequestAborted);

        context.Response.StatusCode = (int)response.StatusCode;

        foreach (var header in response.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        foreach (var header in response.Content.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        context.Response.Headers.Remove("transfer-encoding");

        await response.Content.CopyToAsync(context.Response.Body);
    }

    private void InitializeDatabase()
    {
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
    }

    private sealed class IdentityQueryContextAdapter(
        IdentityDbContext dbContext,
        AuthorizationDbContext authorizationDbContext) : IIdentityQueryContext
    {
        public IQueryable<User> UsersQueryable() => dbContext.Users.AsNoTracking();

        public IQueryable<long> ActiveUserIdsByIncubatorQueryable(long incubatorId) =>
            authorizationDbContext.RoleAssignments
                .AsNoTracking()
                .Where(ra => ra.IncubatorId == incubatorId && ra.IsActive)
                .Select(ra => ra.UserId)
                .Distinct();
    }
}
