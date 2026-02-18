# Astrolabe Templates

## Installation

```bash
dotnet new install Astrolabe.Templates
```

This installs two templates:
- `astrolabe` - Full-stack application (backend + frontend + Aspire)
- `astrolabe-site` - Add a new Next.js site to an existing Astrolabe project

---

## Full Stack Template (`astrolabe`)

### Usage

```bash
dotnet new astrolabe -n MyProject -o ./MyProject \
  --SolutionName MySolution \
  --SiteName my-site \
  --IncludeDemoData true
```

### Parameters

- `-n|--name` - Project name (required)
- `-o|--output` - Output directory (default: current)
- `--SolutionName` - Solution name (default: same as project name)
- `--SiteName` - Next.js site name (default: dashboard)
- `--HttpPort` - Backend HTTP port (default: 5000)
- `--HttpsPort` - Backend HTTPS port (default: 5001)
- `--SpaPort` - SPA dev server port (default: 8000)
- `--ConnectionString` - Database connection string
- `--IncludeDemoData` - Include Tea CRUD demo (default: true)
- `--IncludeOrleans` - Include Orleans distributed actor framework with tea-themed demo (default: false)
- `--SkipSetup` - Skip automatic setup (default: false)

### Examples

**Create skeleton project**:

```bash
dotnet new astrolabe -n MyApp --IncludeDemoData false
```

**Create with Orleans support**:

```bash
dotnet new astrolabe -n MyApp --IncludeOrleans true
```

**Create with custom ports**:

```bash
dotnet new astrolabe -n MyApp --HttpPort 6000 --HttpsPort 6001
```

**Skip orchestration** (manual setup):

```bash
dotnet new astrolabe -n MyApp --SkipSetup true
cd MyApp
dotnet build
cd ClientApp && npx @microsoft/rush@5.153.2 update
# ... manual steps
```

---

## Site Template (`astrolabe-site`)

Add a new Next.js site to an existing Astrolabe project created with `dotnet new astrolabe`.

### Usage

```bash
# From the solution root of an existing Astrolabe project:
dotnet new astrolabe-site --SiteName admin --SpaPort 8001 -o ClientApp/sites
```

This creates `ClientApp/sites/admin/` with a complete Next.js site, registers it in `rush.json`, and runs `rush update`.

### Parameters

- `--SiteName` - Site folder and package name (default: admin)
- `--SpaPort` - Dev server port (default: 8001)
- `--HttpsPort` - Backend HTTPS port to connect to (default: 5001)
- `--IncludeDemoData` - Include Tea demo pages (default: false)
- `--IncludeOrleans` - Include Orleans tearoom page (default: false)
- `--IncludeLocalUsers` - Include authentication pages (default: true)
- `--SkipSetup` - Skip rush.json registration and dependency install (default: false)

### Examples

**Add a minimal admin site**:

```bash
dotnet new astrolabe-site --SiteName admin --SpaPort 8001 -o ClientApp/sites
```

**Add a site with demo data pages**:

```bash
dotnet new astrolabe-site --SiteName portal --SpaPort 8002 --IncludeDemoData true -o ClientApp/sites
```

**Add a site without auto-setup** (manual):

```bash
dotnet new astrolabe-site --SiteName admin --SkipSetup true -o ClientApp/sites
# Then manually:
# 1. Add {"packageName": "admin", "projectFolder": "sites/admin"} to ClientApp/rush.json
# 2. Run 'rush update' from ClientApp/
# 3. Add frontend executable to AppHost Program.cs
```

### AppHost Integration

After creating the site, add it to your Aspire AppHost `Program.cs`:

```csharp
builder
    .AddExecutable("admin", "rushx", "../ClientApp/sites/admin", ["dev"])
    .WithEnvironment("NEXT_PUBLIC_API_URL", api.GetEndpoint("https"))
    .WithEnvironment("PORT", 8001.ToString())
    .WithHttpEndpoint(port: 8001, name: "http", isProxied: false)
    .WithReference(api);
```
