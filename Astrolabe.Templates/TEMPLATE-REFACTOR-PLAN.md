# Astrolabe .NET Template Implementation Plan

## Context: Why Migrate from Custom Template Generator?

**Important to note: Astrolabe.TestTemplate is TOTALLY DIFFERENT. This is for the Astrolabe.Templates / Astrolabe.TemplateGenerator package. DO NOT CONFUSE THE TWO. THERE ARE NO CARS IN THIS TEMPLATE. DO NOT ADD CARS TO THIS TEMPLATE.**

### Original Implementation

We previously had a custom template generator at `Astrolabe.TemplateGenerator` that used:

- Spectre.Console for interactive prompts
- Manual file copying with `Directory.CreateDirectory()` and `File.Copy()`
- String replacement for template parameters
- Custom orchestration logic embedded in the generator

### Problems with Custom Approach

1. **Non-Standard**: Users expect `dotnet new <template-name>` workflow
2. **Discovery**: Custom tools aren't discoverable via `dotnet new list`
3. **Distribution**: No standard way to distribute via NuGet
4. **IDE Integration**: IDEs don't know about custom generators
5. **Maintenance**: We maintain infrastructure that already exists in .NET SDK

### Decision: Use Official .NET Templating

The .NET SDK includes a robust templating engine at `dotnet new` that provides:

- Standard CLI interface: `dotnet new <template> -n MyProject`
- NuGet distribution: Templates as packages
- Symbol substitution: Built-in parameter replacement
- Conditional processing: Include/exclude content based on parameters
- Post-actions: Run scripts after generation
- IDE integration: Visual Studio, VS Code, Rider all support it

**Reference**: https://learn.microsoft.com/en-us/dotnet/core/tools/custom-templates

## Project Overview: What Are We Templating?

### Astrolabe Full-Stack Application

A complete application template consisting of:

**Backend (.NET 8)**

- ASP.NET Core Web API
- Entity Framework Core with SQL Server
- Astrolabe forms framework for declarative forms
- Code generation controller for TypeScript client types
- Custom reflection utilities for form metadata

**Frontend (Next.js + Rush Monorepo)**

- Rush monorepo with multiple packages:
  - `astrolabe-ui` - Base UI components (Radix UI + Tailwind)
  - `client-common` - Shared utilities and generated types
  - `sites/<site-name>` - Next.js application
- TypeScript code generation from backend C# models
- Form rendering using @react-typed-forms
- Tailwind CSS styling

**Complexity Requirements:**

1. **Multi-step Setup**: Backend build → Rush init → TypeScript generation → Frontend deps
2. **Conditional Content**: Demo (Tea CRUD example) vs Skeleton (empty structure)
3. **Dynamic Naming**: Project files must be renamed based on user input
4. **Parameter Propagation**: Setup orchestration needs access to template parameters

## Architecture Decision: Template + Self-Contained Orchestrator

### The Challenge: Post-Action Limitations

.NET template post-actions have a critical limitation:

```json
{
  "postActions": [
    {
      "actionId": "3A7C4B45-1F5D-4A30-959A-51B88E82B5D2",
      "args": {
        "executable": "dotnet",
        "args": "run --project Setup.csproj"
      }
    }
  ]
}
```

**Problem**: The `args` field is a **static string** that doesn't support parameter substitution. We cannot do:

```json
"args": "run --project Setup.csproj --ProjectName __ProjectName__"  // DOESN'T WORK!
```

This means we can't pass template parameters to the orchestration script via command-line arguments.

### Our Solution: Configuration File + Self-Contained Orchestrator

**Pattern**:

1. Template includes an `astrolabe-setup/` folder with:
   - `Setup.csproj` - Executable C# project
   - `Program.cs` - Entry point
   - `SetupOrchestrator.cs` - Orchestration logic
   - `setup-config.json` - Configuration file with template parameters
2. `setup-config.json` uses template parameter substitution
3. Post-action runs `dotnet run --project astrolabe-setup/Setup.csproj`
4. Setup reads `setup-config.json` to get parameters
5. After completion, setup deletes itself

**Why C# Instead of Shell Script?**

- **Cross-platform**: Works on Windows, Linux, macOS without separate scripts
- **Type-safe**: Configuration deserialization with strong typing
- **Error handling**: Robust exception handling and logging
- **Process management**: Easy to start backend, wait for it, then stop it
- **Reusable code**: Can use same utilities as main project

### Configuration File Example

`astrolabe-setup/setup-config.json`:

```json
{
  "projectName": "AstrolabeApp",
  "solutionName": "__SolutionName__",
  "httpPort": "__HttpPort__",
  "httpsPort": "__HttpsPort__",
  "spaPort": "__SpaPort__",
  "includeDemoData": "__IncludeDemoDataJson__"
}
```

Notice:

- `"AstrolabeApp"` uses `sourceName` replacement (the `-n` parameter value)
- `"__SolutionName__"` uses symbol replacement
- `"__HttpPort__"` is a string because JSON requires quotes around values
- `"__IncludeDemoDataJson__"` is a generated symbol that lowercases the boolean

## Template Structure Design

### Option 1: File-Based Exclusion (Current, Suboptimal)

```
content/astrolabe-fullstack/
├── Controllers/
│   └── CodeGenController.cs        # Skeleton version
├── Forms/
│   └── AppForms.cs                 # Skeleton version
├── ClientApp/
│   └── sites/
│       └── __SiteName__/
│           └── src/
│               └── app/
│                   └── page.tsx    # Skeleton version
└── Demo/                           # Overlay source
    ├── Controllers/
    │   └── TeasController.cs       # Demo-only
    ├── Models/
    │   └── Tea.cs                  # Demo-only
    ├── Forms/
    │   └── AppForms.cs             # Demo version (DUPLICATE!)
    └── ClientApp/
        └── sites/
            └── __SiteName__/
                └── src/
                    └── app/
                        └── page.tsx # Demo version (DUPLICATE!)
```

**template.json configuration**:

```json
"sources": [
  {
    "modifiers": [
      { "exclude": ["Demo/**/*"] }  // Exclude Demo by default
    ]
  },
  {
    "source": "Demo/",
    "target": "./",
    "condition": "(IncludeDemoData)"  // Overlay Demo files if true
  }
]
```

**Problems**:

1. **Duplicate Files**: `AppForms.cs` exists in two places
2. **Maintenance**: Changes to shared code need updates in two files
3. **Drift Risk**: Demo and skeleton versions can diverge
4. **Not DRY**: Violates Don't Repeat Yourself
5. **Unclear Diff**: Hard to see what's actually different

### Option 2: Conditional Processing (Recommended)

**Reference**: https://github.com/dotnet/templating/wiki/Conditional-processing-and-comment-syntax

Use conditional comments to have **one unified codebase** where demo-specific sections are included/excluded based on parameters.

```
content/astrolabe-fullstack/
├── Controllers/
│   ├── CodeGenController.cs
│   └── TeasController.cs           # Excluded if !IncludeDemoData
├── Models/
│   └── Tea.cs                      # Excluded if !IncludeDemoData
├── Forms/
│   └── AppForms.cs                 # Single file with conditionals
└── ClientApp/
    └── sites/
        └── __SiteName__/
            └── src/
                └── app/
                    ├── page.tsx    # Single file with conditionals
                    └── tea/        # Excluded if !IncludeDemoData
                        └── TeaCard.tsx
```

**Single Unified `Forms/AppForms.cs`**:

```csharp
namespace __Namespace__;

public class AppForms
{
//#if (IncludeDemoData)
    public static void Register(IServiceCollection services)
    {
        services.AddScoped<TeaService>();
        services.AddScoped<TeaSearchPage>();
        services.AddScoped<TeaEdit>();
    }
//#else
    // Empty - add your own form registrations here
//#endif
}
```

**Single Unified `ClientApp/sites/__SiteName__/src/app/page.tsx`**:

```typescript
//#if (IncludeDemoData)
import { TeaCard } from "./tea/TeaCard";
//#endif

export default function Home() {
  return (
    <div>
      <h1>Welcome to __SolutionName__</h1>
      //#if (IncludeDemoData)
      <TeaCard />
      //#else
      <p>Get started by editing this page.</p>
      //#endif
    </div>
  );
}
```

**Benefits**:

1. ✅ **Single Source**: One file per component
2. ✅ **Clear Intent**: Easy to see what's demo-specific
3. ✅ **Maintainable**: Changes automatically apply to both variants
4. ✅ **Standard**: Uses official .NET templating feature
5. ✅ **Less Code**: Smaller template package

## Conditional Processing Syntax Reference

### By File Type

| Language              | Opening                  | Closing         | Example                        |
| --------------------- | ------------------------ | --------------- | ------------------------------ |
| C#                    | `//#if (condition)`      | `//#endif`      | `//#if (IncludeDemoData)`      |
| TypeScript/JavaScript | `//#if (condition)`      | `//#endif`      | `//#if (IncludeDemoData)`      |
| JSON                  | `//:::#if (condition)`   | `//:::#endif`   | `//:::#if (IncludeDemoData)`   |
| XML/HTML              | `<!--#if (condition)-->` | `<!--#endif-->` | `<!--#if (IncludeDemoData)-->` |

### Whitespace Control

```csharp
// Standard - keeps line as comment when false
//#if (IncludeDemoData)
public class Tea { }
//#endif

// Remove entire line when condition is false
///-#if (IncludeDemoData)
public class Tea { }
///-#endif

// Remove line when condition is true (inverse)
///+#if (!IncludeDemoData)
// This line only appears when demo data is excluded
///+#endif
```

### Conditionals with Else

```csharp
//#if (IncludeDemoData)
    public static void RegisterDemo(IServiceCollection services)
    {
        services.AddScoped<TeaService>();
    }
//#else
    // Add your own services here
//#endif
```

### Nesting

```csharp
//#if (IncludeDemoData)
    public DbSet<Tea> Teas { get; set; }

    //#if (UsePostgres)
    // PostgreSQL-specific configuration
    //#else
    // SQL Server configuration
    //#endif
//#endif
```

## Template Configuration: template.json

### Location

`.template.config/template.json` in the template content root.

### Complete Example

```json
{
  "$schema": "http://json.schemastore.org/template",
  "author": "Astrolabe Team",
  "classifications": ["Web", "Full Stack", "React", "ASP.NET Core", "Next.js"],
  "identity": "Astrolabe.FullStack.Template",
  "name": "Astrolabe Full Stack Application",
  "shortName": "astrolabe",
  "description": "A full-stack application template with ASP.NET Core backend and Next.js frontend using the Astrolabe framework",
  "sourceName": "AstrolabeApp",
  "preferNameDirectory": true,

  "symbols": {
    "ProjectName": {
      "type": "parameter",
      "datatype": "text",
      "defaultValue": "AstrolabeApp",
      "replaces": "__ProjectName__",
      "fileRename": "__ProjectName__",
      "description": "The name of the project"
    },
    "SolutionName": {
      "type": "parameter",
      "datatype": "text",
      "defaultValue": "",
      "description": "The name of the solution (defaults to ProjectName if empty)"
    },
    "SolutionNameComputed": {
      "type": "generated",
      "generator": "coalesce",
      "parameters": {
        "sourceVariableName": "SolutionName",
        "fallbackVariableName": "ProjectName"
      },
      "replaces": "__SolutionName__"
    },
    "IncludeDemoData": {
      "type": "parameter",
      "datatype": "bool",
      "defaultValue": "true",
      "description": "Include demo data (Tea model, controller, and UI pages)"
    },
    "IncludeDemoDataLower": {
      "type": "generated",
      "generator": "casing",
      "parameters": {
        "source": "IncludeDemoData",
        "toLower": true
      },
      "replaces": "__IncludeDemoDataJson__"
    }
  },

  "sources": [
    {
      "modifiers": [
        {
          "exclude": [
            "**/bin/**",
            "**/obj/**",
            "**/node_modules/**",
            "**/.next/**"
          ]
        },
        {
          "condition": "(!IncludeDemoData)",
          "exclude": [
            "Models/Tea.cs",
            "Controllers/TeasController.cs",
            "Data/DbSeeder.cs",
            "Forms/TeaEdit.cs",
            "Forms/TeaSearchPage.cs",
            "Services/TeaService.cs",
            "ClientApp/sites/__SiteName__/src/app/tea/**"
          ]
        },
        {
          "condition": "(SkipSetup)",
          "exclude": ["astrolabe-setup/**/*"]
        }
      ],
      "rename": {
        "AstrolabeApp.csproj": "__ProjectName__.csproj"
      }
    }
  ],

  "postActions": [
    {
      "condition": "(!SkipSetup)",
      "description": "Running Astrolabe project setup (build, Rush init, code generation, etc.)",
      "actionId": "3A7C4B45-1F5D-4A30-959A-51B88E82B5D2",
      "continueOnError": false,
      "args": {
        "executable": "dotnet",
        "args": "run --project astrolabe-setup/Setup.csproj"
      }
    }
  ]
}
```

### Key Concepts Explained

**sourceName**:

- The string `"AstrolabeApp"` throughout the template is replaced by the `-n` parameter value
- Example: `dotnet new astrolabe -n TeaApp` replaces all `AstrolabeApp` → `TeaApp`
- Used for file naming (via `rename`) and in code

**preferNameDirectory**:

- When true, creates parent directory with the name
- `dotnet new astrolabe -n MyApp` creates `MyApp/` directory

**Symbol Types**:

- `parameter` - User-provided value (via `--ParamName value`)
- `generated` - Computed from other symbols
- `computed` - Boolean expression (but can't use `replaces`)

**Generated Symbol Generators**:

- `coalesce` - Use first non-empty value: `SolutionName ?? ProjectName`
- `casing` - Change case: `toLowerCase`, `toUpperCase`, `firstLetterUpperCase`, etc.
- `regex` - Pattern-based transformation

**Sources Configuration**:

- `modifiers` - Apply conditions to include/exclude files
- `exclude` - Glob patterns for files to exclude
- `condition` - When to apply the modifier
- `rename` - Map old filename to new filename (supports symbol replacement)

**Post-Actions**:

- `actionId: 3A7C4B45-1F5D-4A30-959A-51B88E82B5D2` - Standard "run script/command" action
- `executable` + `args` - Command to run
- `condition` - When to run the action
- `continueOnError` - Whether to proceed if action fails

## File Naming Strategy

### Problem: Dynamic File Naming

We need:

- `MyProject.csproj` (not `AstrolabeApp.csproj`)
- `sites/my-site/` (not `sites/__SiteName__/`)

### Solution 1: sourceName Replacement (Simple Files)

For files directly named after the project:

**In template**: `AstrolabeApp.csproj`

**In template.json**:

```json
{
  "sourceName": "AstrolabeApp",
  "sources": [
    {
      "rename": {
        "AstrolabeApp.csproj": "__ProjectName__.csproj"
      }
    }
  ]
}
```

This renames `AstrolabeApp.csproj` → `TeaApp.csproj` when using `-n TeaApp`.

### Solution 2: Symbol Replacement (Parameterized Files)

For files with custom parameter names:

**In template**: `sites/__SiteName__/`

**In template.json**:

```json
{
  "symbols": {
    "SiteName": {
      "type": "parameter",
      "datatype": "text",
      "defaultValue": "myapp-site",
      "fileRename": "__SiteName__"
    }
  }
}
```

The `fileRename` property causes folders/files with `__SiteName__` in their path to be renamed.

**Usage**: `dotnet new astrolabe -n MyApp --SiteName my-site`
**Result**: `sites/my-site/` directory created

## Packaging and Distribution

### Package Structure

```
Astrolabe.Templates/
├── Astrolabe.Templates.csproj   # Package project
├── content/
│   └── astrolabe-fullstack/     # Template content
│       ├── .template.config/
│       │   └── template.json
│       ├── Controllers/
│       ├── Data/
│       ├── Models/
│       ├── ClientApp/
│       └── astrolabe-setup/
└── README.md
```

### Astrolabe.Templates.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <!-- Template Package Configuration -->
    <PackageType>Template</PackageType>
    <PackageVersion>1.0.0</PackageVersion>
    <PackageId>Astrolabe.Templates</PackageId>
    <Title>Astrolabe Full Stack Application Templates</Title>
    <Authors>Astrolabe Team</Authors>
    <Description>Official .NET templates for creating Astrolabe full-stack applications</Description>

    <!-- Required for template packages -->
    <TargetFramework>netstandard2.0</TargetFramework>
    <IncludeContentInPack>true</IncludeContentInPack>
    <IncludeBuildOutput>false</IncludeBuildOutput>
    <ContentTargetFolders>content</ContentTargetFolders>
    <NoDefaultExcludes>true</NoDefaultExcludes>
  </PropertyGroup>

  <ItemGroup>
    <Content Include="content/**/*" />
    <Compile Remove="**/*" />
  </ItemGroup>
</Project>
```

**Key Properties**:

- `PackageType>Template` - Identifies this as a template package
- `IncludeContentInPack>true` - Include content/ folder in package
- `IncludeBuildOutput>false` - Don't include DLLs (templates don't build)
- `ContentTargetFolders>content` - Where to place content in package
- `NoDefaultExcludes>true` - Include hidden files (like `.gitignore`, `.editorconfig`)

### Build and Install

```bash
# Build the package
dotnet pack Astrolabe.Templates/Astrolabe.Templates.csproj -o ./nupkg

# Install locally for testing
dotnet new install ./nupkg/Astrolabe.Templates.1.0.0.nupkg

# Uninstall
dotnet new uninstall Astrolabe.Templates

# List installed templates
dotnet new list astrolabe
```

### NuGet Publishing

```bash
# Push to NuGet.org
dotnet nuget push ./nupkg/Astrolabe.Templates.1.0.0.nupkg \
  --api-key YOUR_API_KEY \
  --source https://api.nuget.org/v3/index.json

# Users install from NuGet
dotnet new install Astrolabe.Templates
```

## Setup Orchestrator Implementation

### Why It's Needed

The Astrolabe template requires complex initialization:

1. **Backend Build**: Must build C# project to get assemblies for code generation
2. **Rush Initialization**: Initialize Rush monorepo (`rush update`)
3. **TypeScript Generation**:
   - Start backend server
   - Wait for it to be ready (health check)
   - Run `npm run gencode` to fetch Swagger and generate TypeScript types
   - Run `npm run geneditorschemas` to generate form schemas
   - Stop backend server
4. **Frontend Dependencies**: Install frontend packages via Rush

These steps have **dependencies** (each requires previous to complete) and involve **process management** (start/stop servers, wait for readiness).

### Architecture

**Three Files**:

1. **Setup.csproj** - Minimal executable project
2. **Program.cs** - Entry point, reads config, invokes orchestrator
3. **SetupOrchestrator.cs** - Core orchestration logic
4. **setup-config.json** - Configuration with template parameters

### Setup.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="System.Text.Json" Version="8.0.0" />
  </ItemGroup>

  <ItemGroup>
    <None Update="setup-config.json">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </None>
  </ItemGroup>
</Project>
```

**Critical**: The `<None Update="setup-config.json">` ensures config file is copied to `bin/Debug/net8.0/` so it can be read at runtime.

### Program.cs

```csharp
using System.Text.Json;
using Astrolabe.Setup;

var configPath = Path.Combine(AppContext.BaseDirectory, "setup-config.json");

if (!File.Exists(configPath))
{
    Console.Error.WriteLine($"Setup failed: Could not find file '{configPath}'.");
    return 1;
}

var configJson = await File.ReadAllTextAsync(configPath);
var config = JsonSerializer.Deserialize<SetupConfig>(configJson);

if (config == null)
{
    Console.Error.WriteLine("Setup failed: Could not parse setup-config.json");
    return 1;
}

var orchestrator = new SetupOrchestrator(config);
await orchestrator.RunSetup();

// Self-destruct: Delete setup folder after successful completion
await Task.Delay(1000);
var setupDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "astrolabe-setup");
if (Directory.Exists(setupDir))
{
    Directory.Delete(setupDir, recursive: true);
}

return 0;
```

**Self-Destruction**: After successful setup, deletes the `astrolabe-setup/` folder since it's no longer needed.

### SetupOrchestrator.cs (Abbreviated)

```csharp
public class SetupOrchestrator
{
    private readonly SetupConfig _config;
    private readonly string _projectRoot;

    public async Task RunSetup()
    {
        var steps = new List<(string Name, Func<Task> Action)>
        {
            ("Building backend", BuildBackend),
            ("Initializing Rush", InitializeRush),
            ("Generating TypeScript client", GenerateTypeScriptClient),
            ("Installing frontend dependencies", InstallFrontendDependencies)
        };

        if (_config.IncludeDemoData)
        {
            steps.Insert(3, ("Seeding database", SeedDatabase));
        }

        for (int i = 0; i < steps.Count; i++)
        {
            var (name, action) = steps[i];
            Console.WriteLine($"[{i + 1}/{steps.Count}] {name}...");
            await action();
            Console.WriteLine($"✓ {name} completed\\n");
        }
    }

    private async Task GenerateTypeScriptClient()
    {
        // Start backend in background
        var backendProcess = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run",
            WorkingDirectory = _projectRoot,
            // ... redirect output
        });

        // Wait for backend to be ready (health check)
        await WaitForBackendReady();

        // Run code generation
        var clientCommonPath = Path.Combine(_projectRoot, "ClientApp", "client-common");
        await RunCommand("npm", "run gencode", clientCommonPath);
        await RunCommand("npm", "run geneditorschemas", clientCommonPath);

        // Stop backend
        backendProcess?.Kill(entireProcessTree: true);
    }

    private async Task WaitForBackendReady()
    {
        using var httpClient = new HttpClient();
        for (int i = 0; i < 30; i++)
        {
            try
            {
                var response = await httpClient.GetAsync(
                    $"http://localhost:{_config.HttpPort}/swagger/v1/swagger.json"
                );
                if (response.IsSuccessStatusCode) return;
            }
            catch { }
            await Task.Delay(1000);
        }
        throw new Exception("Backend failed to start");
    }
}
```

### SetupConfig.cs

```csharp
public class SetupConfig
{
    public string ProjectName { get; set; } = "";
    public string SolutionName { get; set; } = "";
    public int HttpPort { get; set; }
    public int HttpsPort { get; set; }
    public int SpaPort { get; set; }
    public string SiteName { get; set; } = "";
    public bool IncludeDemoData { get; set; }
}
```

## Implementation Plan

### Phase 1: Create Package Structure

```bash
mkdir -p Astrolabe.Templates/content/astrolabe-fullstack/.template.config
```

**Files to create**:

1. `Astrolabe.Templates/Astrolabe.Templates.csproj` - Package project
2. `content/astrolabe-fullstack/.template.config/template.json` - Template config

### Phase 2: Copy Skeleton Content

Copy the "Skeleton" version of the application (without demo data) to `content/astrolabe-fullstack/`:

- Backend files (Controllers, Data, Forms, Models, Services, etc.)
- ClientApp structure (astrolabe-ui, client-common, sites)
- Configuration files (.gitignore, appsettings.json, etc.)

### Phase 3: Identify Demo vs Skeleton Differences

Compare the Demo and Skeleton versions to identify:

1. **Demo-only files** - Files that don't exist in skeleton (e.g., `Tea.cs`, `TeasController.cs`)
2. **Files with differences** - Files that exist in both but have different content (e.g., `AppForms.cs`, `page.tsx`)

Create a checklist:

**Demo-Only Files (Exclude when `!IncludeDemoData`)**:

- [ ] Models/Tea.cs
- [ ] Controllers/TeasController.cs
- [ ] Data/DbSeeder.cs
- [ ] Forms/TeaEdit.cs
- [ ] Forms/TeaSearchPage.cs
- [ ] Forms/FieldOption.cs
- [ ] Services/TeaService.cs
- [ ] ClientApp/sites/\_\_SiteName\_\_/src/app/tea/ (folder)

**Files to Merge with Conditionals**:

- [ ] Forms/AppForms.cs
- [ ] Data/EF/AppDbContext.cs
- [ ] Program.cs
- [ ] ClientApp/sites/\_\_SiteName\_\_/src/app/page.tsx
- [ ] ClientApp/sites/\_\_SiteName\_\_/src/app/layout.tsx
- [ ] ClientApp/sites/\_\_SiteName\_\_/src/routes.tsx

### Phase 4: Merge Files with Conditional Syntax

For each file in the "Files to Merge" list:

1. **Read skeleton version**
2. **Read demo version**
3. **Identify differences** (use `git diff` or side-by-side comparison)
4. **Add conditional blocks** using `//#if (IncludeDemoData)`
5. **Test both variants**

Example process for `Forms/AppForms.cs`:

```bash
# View skeleton version (what's currently in template)
cat content/astrolabe-fullstack/Forms/AppForms.cs

# View demo version (from original)
cat <path-to-original>/Demo/Forms/AppForms.cs

# Edit to add conditionals
code content/astrolabe-fullstack/Forms/AppForms.cs
```

### Phase 5: Add Demo-Only Files

Copy demo-only files to the template:

```bash
cp <original>/Demo/Models/Tea.cs content/astrolabe-fullstack/Models/
cp <original>/Demo/Controllers/TeasController.cs content/astrolabe-fullstack/Controllers/
# ... etc
```

Update `template.json` to exclude these when `!IncludeDemoData`:

```json
{
  "condition": "(!IncludeDemoData)",
  "exclude": [
    "Models/Tea.cs",
    "Controllers/TeasController.cs",
    "Data/DbSeeder.cs",
    "Forms/TeaEdit.cs",
    "Forms/TeaSearchPage.cs",
    "Forms/FieldOption.cs",
    "Services/TeaService.cs",
    "ClientApp/sites/__SiteName__/src/app/tea/**"
  ]
}
```

### Phase 6: Create Setup Orchestrator

1. Create `content/astrolabe-fullstack/astrolabe-setup/` directory
2. Copy orchestration logic from `Astrolabe.TemplateGenerator/TemplateGenerator.cs`
3. Refactor into:
   - `Setup.csproj`
   - `Program.cs`
   - `SetupOrchestrator.cs`
   - `SetupConfig.cs`
   - `setup-config.json` (template with parameter placeholders)

### Phase 7: Configure template.json

Complete the `template.json` with:

- All symbols (ProjectName, SolutionName, HttpPort, etc.)
- Generated symbols (SolutionNameComputed, IncludeDemoDataLower, etc.)
- Sources configuration with exclusions
- File renames
- Post-action for setup orchestrator

### Phase 8: Test Template

```bash
# Build package
dotnet pack Astrolabe.Templates/Astrolabe.Templates.csproj -o ./nupkg

# Install
dotnet new install ./nupkg/Astrolabe.Templates.1.0.0.nupkg

# Test skeleton generation
dotnet new astrolabe -n TestSkeleton -o test-output/TestSkeleton \
  --IncludeDemoData false --SkipSetup true

# Verify: No Tea files, minimal AppForms, basic page.tsx
cd test-output/TestSkeleton
dotnet build
cd ../..

# Test demo generation
dotnet new astrolabe -n TestDemo -o test-output/TestDemo \
  --IncludeDemoData true --SkipSetup true

# Verify: Tea files present, populated AppForms, TeaCard in page.tsx
cd test-output/TestDemo
dotnet build
cd ../..

# Test full generation with setup
dotnet new astrolabe -n FullTest -o test-output/FullTest \
  --SolutionName TestSolution --SiteName test-site \
  --IncludeDemoData true --SkipSetup false

# Should run full orchestration
```

### Phase 9: Handle Edge Cases

**Namespace Sanitization**:
User might enter project name with invalid characters. Create generated symbol:

```json
"ProjectNameSanitized": {
  "type": "generated",
  "generator": "regex",
  "parameters": {
    "source": "ProjectName",
    "steps": [
      { "regex": "[^a-zA-Z0-9_]", "replacement": "_" },
      { "regex": "^([0-9])", "replacement": "_$1" }
    ]
  },
  "replaces": "__Namespace__"
}
```

**Port Conflicts**:
Allow users to specify ports to avoid conflicts:

```bash
dotnet new astrolabe -n MyApp \
  --HttpPort 5010 \
  --HttpsPort 5011 \
  --SpaPort 8001
```

**Database Connection**:
Default connection string but allow override:

```bash
dotnet new astrolabe -n MyApp \
  --ConnectionString "Server=localhost;Database=MyDb;..."
```

### Phase 10: Documentation

Create `README.md` for the template package:

````markdown
# Astrolabe Full Stack Template

## Installation

```bash
dotnet new install Astrolabe.Templates
```
````

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
- `--SiteName` - Next.js site name (default: myapp-site)
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

```

## Common Issues and Solutions

### Issue 1: Post-Action Prompts for Confirmation

**Problem**: Running without `--allow-scripts yes` causes interactive prompt:
```

Do you want to run this action [Y(yes)|N(no)]?

```

**Solution**: Add to instructions or make post-action optional via parameter.

### Issue 2: Setup Config Not Found

**Problem**:
```

Setup failed: Could not find file 'setup-config.json'

````

**Cause**: `Setup.csproj` doesn't copy `setup-config.json` to output directory.

**Solution**: Add to `.csproj`:
```xml
<ItemGroup>
  <None Update="setup-config.json">
    <CopyToOutputDirectory>Always</CopyToOutputDirectory>
  </None>
</ItemGroup>
````

### Issue 3: Folder Named ".astrolabe-setup" Excluded from Package

**Problem**: Dot-prefixed folders treated as hidden and excluded during `dotnet pack`.

**Solution**: Rename to `astrolabe-setup` (without dot) and update all references.

### Issue 4: File Not Renamed

**Problem**: `.csproj` stays as `AstrolabeApp.csproj` instead of `TeaApp.csproj`.

**Cause**: `sourceName` replacement doesn't apply to filenames automatically.

**Solution**: Add explicit rename:

```json
"rename": {
  "AstrolabeApp.csproj": "__ProjectName__.csproj"
}
```

## Testing Checklist

- [ ] Template installs successfully
- [ ] Skeleton generation (`--IncludeDemoData false`)
  - [ ] No Tea model/controller/forms
  - [ ] AppForms is minimal
  - [ ] Home page has placeholder content
  - [ ] Builds successfully
- [ ] Demo generation (`--IncludeDemoData true`)
  - [ ] Tea model/controller/forms present
  - [ ] AppForms registers Tea services
  - [ ] Home page has TeaCard
  - [ ] Builds successfully
- [ ] File renaming works
  - [ ] `.csproj` renamed to project name
  - [ ] Site folder renamed to `--SiteName` value
- [ ] Setup orchestration works
  - [ ] Backend builds
  - [ ] Rush initializes
  - [ ] TypeScript generation succeeds
  - [ ] Frontend dependencies install
  - [ ] Setup folder deletes itself
- [ ] Parameters work
  - [ ] Custom ports applied
  - [ ] Custom connection string applied
  - [ ] Solution name defaults to project name
  - [ ] Solution name override works

## Success Criteria

✅ Template generates both skeleton and demo variants correctly
✅ No duplicate files between variants
✅ Conditional syntax makes differences clear
✅ Setup orchestration runs automatically (when enabled)
✅ Template package is under 5MB (no node_modules/bin/obj included)
✅ Works on Windows, Linux, macOS
✅ Builds and runs successfully after generation
✅ Documentation is clear and complete

## References

- [Custom templates for dotnet new](https://learn.microsoft.com/en-us/dotnet/core/tools/custom-templates)
- [Conditional processing syntax](https://github.com/dotnet/templating/wiki/Conditional-processing-and-comment-syntax)
- [Template.json reference](https://github.com/dotnet/templating/wiki/Reference-for-template.json)
- [Post-action types](https://github.com/dotnet/templating/wiki/Post-Action-Registry)
