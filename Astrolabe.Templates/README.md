# Astrolabe Full Stack Template

## Installation

```bash
dotnet new install Astrolabe.Templates
```

## Usage

```bash
dotnet new astrolabe -n MyProject -o ./MyProject \
  --SolutionName MySolution \
  --SiteName my-site \
  --IncludeDemoData true
```

## Parameters

- `-n|--name` - Project name (required)
- `-o|--output` - Output directory (default: current)
- `--SolutionName` - Solution name (default: same as project name)
- `--SiteName` - Next.js site name (default: dashboard)
- `--HttpPort` - Backend HTTP port (default: 5000)
- `--HttpsPort` - Backend HTTPS port (default: 5001)
- `--SpaPort` - SPA dev server port (default: 8000)
- `--ConnectionString` - Database connection string
- `--IncludeDemoData` - Include Tea CRUD demo (default: true)
- `--SkipSetup` - Skip automatic setup (default: false)

## Examples

**Create skeleton project**:

```bash
dotnet new astrolabe -n MyApp --IncludeDemoData false
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
