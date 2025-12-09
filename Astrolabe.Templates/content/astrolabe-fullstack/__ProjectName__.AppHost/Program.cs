var builder = DistributedApplication.CreateBuilder(args);

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
var api = builder.AddProject<Projects.__Namespace__>("api")
    .WithReference(db)
#if (ManagedSql)
    .WaitFor(db)
#endif
    ;

#if (IncludeOrleans)
api.WithReference(orleans);
#endif

// Frontend: Next.js dev server via pnpm
// Runs directly in the site directory
builder.AddNpmApp("frontend", "../ClientApp/sites/__SiteName__", "dev")
    .WithEnvironment("NEXT_PUBLIC_API_URL", api.GetEndpoint("https"))
    .WithHttpEndpoint(port: __SpaPort__, name: "http", isProxied: false);

builder.Build().Run();
