# **ProjectName**

A full-stack application built with ASP.NET Core backend and Next.js frontend using the Astrolabe framework.

<!-- SETUP_INSTRUCTIONS_START -->

## 🚀 Initial Setup Required

Run the following command to complete project setup:

```bash
dotnet run --project astrolabe-setup
```

This will:

1. Build the backend
2. Initialize Rush (monorepo package manager)
3. Generate TypeScript client from API
4. Install frontend dependencies

Setup takes approximately 2-3 minutes to complete.

<!-- SETUP_INSTRUCTIONS_END -->

## Development

### Prerequisites

- .NET 8.0 SDK or later
- Node.js 22+
- SQL Server (for database)

### Running the Application

Start the backend:

```bash
dotnet run
```

In a separate terminal, start the frontend:

```bash
cd ClientApp/sites/__SiteName__
rushx dev
```

The frontend will be available at `http://localhost:__SpaPort__` but proxies to the backend at `https://localhost:__HttpsPort__`.

### Project Structure

```
__ProjectName__/
├── Controllers/          # API controllers
├── Data/                 # Database context and configuration
├── Models/               # Entity models
├── Services/             # Business logic services
├── Forms/                # Form definitions for Astrolabe schemas
<!--#if (IncludeOrleans) -->
├── Grains/               # Orleans grain interfaces and implementations
<!--#endif -->
├── ClientApp/            # Frontend monorepo
│   ├── sites/
│   │   └── __SiteName__/    # Next.js application
│   ├── client-common/    # Shared API client and types
│   └── common/           # Shared utilities
└── Migrations/           # EF Core migrations
```

### Available Endpoints

- `https://localhost:__HttpsPort__/swagger` - Swagger API documentation
- `http://localhost:__SpaPort__` - Frontend development server
<!--#if (IncludeOrleans) -->
- `http://localhost:__SpaPort__/tearoom` - Orleans Tea Room demo
<!--#endif -->

### Database

The application uses Entity Framework Core with SQL Server. Configure your connection string in `appsettings.json` or `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Database=__ProjectName__;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

Run migrations:

```bash
dotnet ef database update
```

## Building for Production

```bash
# Build backend
dotnet publish -c Release

# Build frontend
cd ClientApp
rush build
```

<!--#if (IncludeOrleans) -->

## Orleans (Distributed Actors)

This project includes Microsoft Orleans for building distributed, scalable applications using the actor model.

### Tea Room Demo

The Tea Room demo (`/tearoom`) showcases Orleans concepts:

- **Tea Room Grain** (`ITeaRoomGrain`): Manages a tea room with multiple kettles, coordinates orders, and demonstrates grain-to-grain communication
- **Tea Kettle Grain** (`ITeaKettleGrain`): Simulates tea brewing with timers, state transitions (Heating → Steeping → Ready), and persistent state

### Key Orleans Concepts Demonstrated

1. **Virtual Actors (Grains)**: Each kettle and tea room is an independent actor with its own state
2. **Grain Timers**: Brewing simulation uses Orleans timers for realistic async processing
3. **State Persistence**: Grain state persists across restarts using memory storage (configure for production)
4. **Grain-to-Grain Calls**: Tea room coordinates with kettle grains to manage orders

### Production Configuration

For production, replace the in-memory storage with persistent storage:

```csharp
// In Program.cs, replace:
siloBuilder.AddMemoryGrainStorage("teaRoomStore");

// With Azure Table Storage:
siloBuilder.AddAzureTableGrainStorage("teaRoomStore", options =>
{
    options.ConfigureTableServiceClient(connectionString);
});

// Or SQL Server:
siloBuilder.AddAdoNetGrainStorage("teaRoomStore", options =>
{
    options.ConnectionString = connectionString;
    options.Invariant = "Microsoft.Data.SqlClient";
});
```

For clustering in production, replace `UseLocalhostClustering()` with Azure Table Storage or Redis clustering.

<!--#endif -->

## License

[Add your license here]
