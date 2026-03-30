using Mentoory.Aspire.AppHost.AppHost;

const string defaultConnectionName = "DefaultConnection";

IResourceBuilder<IResourceWithConnectionString> dbDefaultConnection;

var builder = DistributedApplication.CreateBuilder(args);

if (builder.ExecutionContext.IsRunMode) //// Running locally
{
    dbDefaultConnection = builder.AddConnectionString(defaultConnectionName);
}
else //// Running in Azure
{
    dbDefaultConnection = builder
        .AddAzureSqlServer(name: "mentoory-dbserver")
        .AddDatabase(name: defaultConnectionName, databaseName: "MentooryDb");
}

var appSettingsJson = AppsettingsLoader.SerializeUserJsonConfiguration(builder.ExecutionContext.IsRunMode, out var appSettingsJsonHash);

builder.AddProject<Projects.Mentoory_Web>("mentoory-web")
    .WithReference(dbDefaultConnection)
    .WithEnvironment("AspireAppsettings", appSettingsJson)
    .WithEnvironment("APPSETTINGS_HASH", appSettingsJsonHash) // <— forces a spec change
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health");

builder.Build().Run();
