using System.Reflection;
using Mentoory.Example.Application;
using Mentoory.Example.Infrastructure;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.Behaviors;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Shared.Infrastructure.Behaviors;
using Mentoory.Shared.Infrastructure.Persistence;
using Mentoory.Web.Infrastructure.Persistence;
using Mentoory.Web.Services;

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

builder.Services.AddScoped<MediatRExecutor>();

builder.Services.AddSingleton<IVersionProvider, VersionProvider>();

builder.Services.AddSharedApplication();

builder.Services.AddExampleApplication();

builder.AddExampleInfrastructure();

// Add services to the container.
builder.Services.AddControllersWithViews();

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

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
