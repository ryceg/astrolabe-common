using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

// Load appsettings.json from solution root to pass configuration to services
var solutionRoot = Path.GetDirectoryName(Path.GetDirectoryName(AppContext.BaseDirectory));
var appSettingsPath = Path.Combine(solutionRoot!, "appsettings.json");
if (File.Exists(appSettingsPath))
{
    builder.Configuration.AddJsonFile(appSettingsPath, optional: false, reloadOnChange: true);

    var envAppSettingsPath = Path.Combine(solutionRoot!, $"appsettings.{builder.Environment.EnvironmentName}.json");
    if (File.Exists(envAppSettingsPath))
    {
        builder.Configuration.AddJsonFile(envAppSettingsPath, optional: true, reloadOnChange: true);
    }
}

#if (ManagedSql)
// Managed SQL Server container (requires Docker)
var sql = builder.AddSqlServer("sql")
    .WithLifetime(ContainerLifetime.Persistent);
var db = sql.AddDatabase("appdb");
#else
// Use existing SQL Server via connection string
var db = builder.AddConnectionString("Default");
#endif

#if (IncludeOrleans)
// Orleans with development clustering
var orleans = builder.AddOrleans("default")
    .WithDevelopmentClustering();
#endif

// API Service (now in nested __ProjectName__ subdirectory)
// Port is dynamically assigned by Aspire
var api = builder.AddProject<Projects.__Namespace__>("api")
    .WithHttpsEndpoint(env: "ASPNETCORE_HTTPS_PORTS")
    .WithHttpEndpoint(env: "ASPNETCORE_HTTP_PORTS")
    .WithReference(db)
#if (ManagedSql)
    .WaitFor(db)
#endif
#if (IncludeOrleans)
    .WithReference(orleans)
#endif
#if (IncludeLocalUsers)
    .WithEnvironment("Auth__PasswordSalt", builder.Configuration["Auth:PasswordSalt"]!)
    .WithEnvironment("Jwt__Key", builder.Configuration["Jwt:Key"]!)
    .WithEnvironment("Jwt__Issuer", builder.Configuration["Jwt:Issuer"]!)
    .WithEnvironment("Jwt__Audience", builder.Configuration["Jwt:Audience"]!)
#endif
    ;

// Frontend: Next.js dev server via Rush monorepo
// Rush monorepos use 'rushx' to run package scripts
// Run 'rush update' from ClientApp directory before starting Aspire to install dependencies
builder
    .AddExecutable("frontend", "rushx", "../ClientApp/sites/__SiteName__", ["dev"])
    .WithEnvironment("NEXT_PUBLIC_API_URL", api.GetEndpoint("https"))
    .WithEnvironment("PORT", __SpaPort__.ToString())
    .WithHttpEndpoint(port: __SpaPort__, name: "http", isProxied: false)
    .WithReference(api);

builder.Build().Run();
