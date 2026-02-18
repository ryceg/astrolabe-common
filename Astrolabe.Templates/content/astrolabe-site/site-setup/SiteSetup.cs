using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

Console.WriteLine("Setting up new Astrolabe site...");
Console.Out.Flush();

// Find config file
var scriptDirectory =
    Path.GetDirectoryName(typeof(Program).Assembly.Location)
    ?? Directory.GetCurrentDirectory();
var configPath = Path.Combine(scriptDirectory, "site-setup-config.json");

if (!File.Exists(configPath))
{
    configPath = Path.Combine(
        Directory.GetCurrentDirectory(),
        "site-setup",
        "site-setup-config.json"
    );
}

if (!File.Exists(configPath))
{
    configPath = Path.Combine(Directory.GetCurrentDirectory(), "site-setup-config.json");
}

if (!File.Exists(configPath))
{
    Console.Error.WriteLine("Setup failed: Could not find site-setup-config.json");
    Console.Error.WriteLine("Searched in:");
    Console.Error.WriteLine(
        $"  - {Path.Combine(scriptDirectory, "site-setup-config.json")}"
    );
    Console.Error.WriteLine(
        $"  - {Path.Combine(Directory.GetCurrentDirectory(), "site-setup", "site-setup-config.json")}"
    );
    Console.Error.WriteLine(
        $"  - {Path.Combine(Directory.GetCurrentDirectory(), "site-setup-config.json")}"
    );
    return 1;
}

var configJson = await File.ReadAllTextAsync(configPath);
var config = JsonSerializer.Deserialize(
    configJson,
    SiteSetupConfigContext.Default.SiteSetupConfig
);

if (config == null)
{
    Console.Error.WriteLine("Setup failed: Could not parse site-setup-config.json");
    return 1;
}

Console.WriteLine($"Setting up site '{config.SiteName}' on port {config.SpaPort}...");
Console.Out.Flush();

// Determine the output directory
// The post-action runs from the template output root (the -o directory)
// So site-setup/ is here, and __SiteName__/ is a sibling folder
var outputDir = Directory.GetCurrentDirectory();

// If we're inside site-setup, go up one level
if (Path.GetFileName(outputDir) == "site-setup")
{
    outputDir = Path.GetDirectoryName(outputDir) ?? outputDir;
}

// Locate rush.json
// outputDir should be ClientApp/sites/ (the -o target)
// rush.json should be at ClientApp/rush.json (one level up)
var rushJsonPath = Path.Combine(outputDir, "..", "rush.json");
rushJsonPath = Path.GetFullPath(rushJsonPath);

if (!File.Exists(rushJsonPath))
{
    // Try two levels up in case output was the site folder itself
    rushJsonPath = Path.Combine(outputDir, "..", "..", "rush.json");
    rushJsonPath = Path.GetFullPath(rushJsonPath);
}

if (!File.Exists(rushJsonPath))
{
    Console.Error.WriteLine("Could not find rush.json. Searched:");
    Console.Error.WriteLine(
        $"  - {Path.GetFullPath(Path.Combine(outputDir, "..", "rush.json"))}"
    );
    Console.Error.WriteLine($"  - {rushJsonPath}");
    Console.Error.WriteLine();
    Console.Error.WriteLine(
        "Make sure you run this template from the project's ClientApp/sites/ directory:"
    );
    Console.Error.WriteLine(
        "  dotnet new astrolabe-site --SiteName admin -o ClientApp/sites"
    );
    return 1;
}

var clientAppDir = Path.GetDirectoryName(rushJsonPath)!;
var siteFolderRelative = $"sites/{config.SiteName}";
var siteAbsolutePath = Path.Combine(clientAppDir, siteFolderRelative);

// Verify site folder was created
if (!Directory.Exists(siteAbsolutePath))
{
    Console.Error.WriteLine($"Site folder not found at: {siteAbsolutePath}");
    Console.Error.WriteLine("The template may not have created files correctly.");
    return 1;
}

// Step 1: Register in rush.json
Console.WriteLine("[1/2] Registering site in rush.json...");
Console.Out.Flush();

try
{
    var rushContent = await File.ReadAllTextAsync(rushJsonPath);
    var rushDoc = JsonNode.Parse(
        rushContent,
        documentOptions: new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
        }
    );

    if (rushDoc == null)
    {
        Console.Error.WriteLine("Failed to parse rush.json");
        return 1;
    }

    var projects = rushDoc["projects"]?.AsArray();
    if (projects == null)
    {
        Console.Error.WriteLine("rush.json does not contain a 'projects' array");
        return 1;
    }

    // Check for duplicate
    var alreadyExists = projects.Any(p =>
        p?["packageName"]?.GetValue<string>() == config.SiteName
    );

    if (alreadyExists)
    {
        Console.WriteLine(
            $"  Site '{config.SiteName}' is already registered in rush.json. Skipping."
        );
    }
    else
    {
        // Verify shared dependencies exist
        var hasClientCommon = projects.Any(p =>
            p?["packageName"]?.GetValue<string>() == "client-common"
        );
        var hasAstrolabeUi = projects.Any(p =>
            p?["packageName"]?.GetValue<string>() == "@astrolabe/ui"
        );

        if (!hasClientCommon)
        {
            Console.WriteLine(
                "  Warning: 'client-common' not found in rush.json. "
                    + "The new site depends on it. Make sure it exists."
            );
        }

        if (!hasAstrolabeUi)
        {
            Console.WriteLine(
                "  Warning: '@astrolabe/ui' not found in rush.json. "
                    + "The new site depends on it. Make sure it exists."
            );
        }

        var newProject = new JsonObject
        {
            ["packageName"] = config.SiteName,
            ["projectFolder"] = siteFolderRelative,
        };
        projects.Add(newProject);

        var writeOptions = new JsonSerializerOptions { WriteIndented = true };
        await File.WriteAllTextAsync(rushJsonPath, rushDoc.ToJsonString(writeOptions));
        Console.WriteLine($"  Added '{config.SiteName}' to rush.json");
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Failed to update rush.json: {ex.Message}");
    Console.Error.WriteLine("You'll need to manually add the project entry.");
}

// Step 2: Run rush update
Console.WriteLine("[2/2] Installing dependencies (rush update)...");
Console.Out.Flush();

try
{
    await RunCommand(
        "npx",
        "-y @microsoft/rush@5.153.2 update --bypass-policy",
        clientAppDir
    );
    Console.WriteLine("  Dependencies installed successfully.");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Failed to run rush update: {ex.Message}");
    Console.Error.WriteLine(
        "You can run 'rush update' manually from the ClientApp directory."
    );
}

// Print instructions for wiring into AppHost
Console.WriteLine();
Console.WriteLine("========================================");
Console.WriteLine("  Site setup complete!");
Console.WriteLine("========================================");
Console.WriteLine();
Console.WriteLine(
    "To add this site to your Aspire AppHost, add the following to your"
);
Console.WriteLine("AppHost Program.cs (before builder.Build().Run()):");
Console.WriteLine();
Console.WriteLine("builder");
Console.WriteLine(
    $"    .AddExecutable(\"{config.SiteName}\", \"rushx\", \"../ClientApp/sites/{config.SiteName}\", [\"dev\"])"
);
Console.WriteLine(
    "    .WithEnvironment(\"NEXT_PUBLIC_API_URL\", api.GetEndpoint(\"https\"))"
);
Console.WriteLine(
    $"    .WithEnvironment(\"PORT\", {config.SpaPort}.ToString())"
);
Console.WriteLine(
    $"    .WithHttpEndpoint(port: {config.SpaPort}, name: \"http\", isProxied: false)"
);
Console.WriteLine("    .WithReference(api);");
Console.WriteLine();
Console.WriteLine("For non-Aspire projects, start the dev server with:");
Console.WriteLine($"  cd ClientApp/sites/{config.SiteName}");
Console.WriteLine("  rushx dev");
Console.WriteLine();

// Self-delete
DeleteSetupFolder(outputDir);

return 0;

// --- Helper methods ---

void DeleteSetupFolder(string baseDir)
{
    var setupFolder = Path.Combine(baseDir, "site-setup");
    if (!Directory.Exists(setupFolder))
        return;

    Directory.SetCurrentDirectory(baseDir);

    try
    {
        Directory.Delete(setupFolder, recursive: true);
        Console.WriteLine("Removed site-setup folder.");
    }
    catch (UnauthorizedAccessException) when (OperatingSystem.IsWindows())
    {
        ScheduleWindowsDeletion(baseDir, setupFolder);
    }
    catch (IOException) when (OperatingSystem.IsWindows())
    {
        ScheduleWindowsDeletion(baseDir, setupFolder);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Note: Could not remove setup folder: {ex.Message}");
        Console.WriteLine("You can manually delete the 'site-setup' folder.");
    }
}

void ScheduleWindowsDeletion(string workDir, string folder)
{
    try
    {
        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c ping 127.0.0.1 -n 2 >nul & rmdir /s /q \"{folder}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workDir,
        };
        Process.Start(psi);
        Console.WriteLine("Setup folder will be removed shortly.");
    }
    catch
    {
        Console.WriteLine("Note: You can manually delete the 'site-setup' folder.");
    }
}

async Task RunCommand(string command, string arguments, string workingDirectory)
{
    var isWindows = OperatingSystem.IsWindows();
    var fileName = command;
    var args = arguments;

    if (isWindows && (command == "npm" || command == "npx" || command == "pnpm"))
    {
        fileName = "cmd.exe";
        args = $"/c {command} {arguments}";
    }

    Console.WriteLine($"  Running: {fileName} {args}");

    var processStartInfo = new ProcessStartInfo
    {
        FileName = fileName,
        Arguments = args,
        WorkingDirectory = workingDirectory,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        RedirectStandardInput = true,
        UseShellExecute = false,
        CreateNoWindow = false,
    };

    using var process = Process.Start(processStartInfo);
    if (process == null)
        throw new Exception($"Failed to start process: {fileName} {args}");

    process.StandardInput.Close();
    process.OutputDataReceived += (sender, e) =>
    {
        if (e.Data != null)
            Console.WriteLine(e.Data);
    };
    process.ErrorDataReceived += (sender, e) =>
    {
        if (e.Data != null)
            Console.Error.WriteLine(e.Data);
    };
    process.BeginOutputReadLine();
    process.BeginErrorReadLine();

    await process.WaitForExitAsync();

    if (process.ExitCode != 0)
        throw new Exception($"Command failed with exit code {process.ExitCode}");
}

partial class Program { }

public class SiteSetupConfig
{
    public string SiteName { get; set; } = "";
    public int SpaPort { get; set; }
}

[JsonSerializable(typeof(SiteSetupConfig))]
[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString
)]
internal partial class SiteSetupConfigContext : JsonSerializerContext { }
