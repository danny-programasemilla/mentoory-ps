using System.Reflection;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Mentoory.Access.Application;
using Mentoory.Access.Infrastructure;
using Mentoory.Example.Application;
using Mentoory.Example.Infrastructure;
using Mentoory.Shared.Application;
using Mentoory.Tenant.Application;
using Mentoory.Tenant.Infrastructure;
using Mentoory.Diagnostic.Application;
using Mentoory.Diagnostic.Infrastructure;
using Mentoory.Notification.Application;
using Mentoory.Notification.Infrastructure;
using Mentoory.Shared.Application.Audit;
using Mentoory.Shared.Application.Behaviors;
using Mentoory.Shared.Application.Interfaces;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Shared.Infrastructure.Audit;
using Mentoory.Shared.Infrastructure.Behaviors;
using Mentoory.Shared.Infrastructure.Persistence;
using Mentoory.Shared.Application.Notifications;
using Mentoory.Shared.Infrastructure.Notifications;
using Mentoory.Shared.Infrastructure.Services;
using Mentoory.Web.Infrastructure.Authentication;
using Mentoory.Web.Infrastructure.Authorization;
using Mentoory.Web.Infrastructure.Menu;
using Mentoory.Web.Infrastructure.Persistence;
using Mentoory.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var mediatRLicenseKey = builder.Configuration.GetValue<string>("MediatR:LicenseKey");
builder.Services.AddMediatR(cfg =>
{
    cfg.LicenseKey = mediatRLicenseKey;

    cfg.RegisterServicesFromAssemblies(Assembly.GetExecutingAssembly());

    cfg.AddOpenBehavior(typeof(ValidatorBehavior<,>));
    cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
});

builder.Services.AddScoped<IDbContextFactory, DbContextFactory>();

builder.Services.AddSingleton<ITimeProvider, DefaultSystemTimeProvider>();
builder.Services.AddSingleton<IEmailLayoutWrapper, EmailLayoutWrapper>();

builder.Services.AddScoped<MediatRExecutor>();

builder.Services.AddSingleton<IVersionProvider, VersionProvider>();

builder.Services.AddSharedApplication();

builder.Services.AddExampleApplication();

builder.AddExampleInfrastructure();

builder.Services.AddAccessApplication();
builder.AddAccessInfrastructure();
builder.Services.AddTenantApplication();
builder.AddTenantInfrastructure();
builder.Services.AddDiagnosticApplication();
builder.AddDiagnosticInfrastructure();
builder.Services.AddNotificationApplication();
builder.AddNotificationInfrastructure();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Access/Login";
        options.LogoutPath = "/Access/Logout";
        options.AccessDeniedPath = "/Access/Login";
        options.Cookie.Name = "Mentoory.Session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.SlidingExpiration = false;
        // Cookie ExpireTimeSpan is a maximum transport-level bound and cannot be made dynamic without app restart.
        // The operative session timeout is enforced server-side by SessionAuthenticationMiddleware
        // using the DB-configured SessionTimeoutHours value.
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorization();

var isDevelopment = builder.Environment.IsDevelopment();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("login", opt =>
    {
        opt.PermitLimit = isDevelopment ? 1000 : 5;
        opt.Window = TimeSpan.FromMinutes(isDevelopment ? 1 : 15);
        opt.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("registration", opt =>
    {
        opt.PermitLimit = isDevelopment ? 1000 : 3;
        opt.Window = TimeSpan.FromMinutes(isDevelopment ? 1 : 15);
        opt.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("password-reset", opt =>
    {
        opt.PermitLimit = isDevelopment ? 1000 : 3;
        opt.Window = TimeSpan.FromMinutes(isDevelopment ? 1 : 60);
        opt.QueueLimit = 0;
    });
});

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});

builder.Services.AddScoped<ITenantContext, TenantContextService>();
builder.Services.AddScoped<IAuditService, AuditService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IMenuService, MenuService>();

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<PasswordResetRequiredFilter>();
});

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseMiddleware<SessionAuthenticationMiddleware>();
app.UseAuthorization();
app.UseTenantContext();
app.UseAntiforgery();
app.UseRateLimiter();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

public partial class Program;

