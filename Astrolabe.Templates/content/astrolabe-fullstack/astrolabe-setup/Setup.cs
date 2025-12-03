using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;



Console.WriteLine("Initializing setup...");
Console.Out.Flush();

// When running with "dotnet run Setup.cs", we need to find setup-config.json
// It will be in the same directory as Setup.cs
var scriptDirectory = Path.GetDirectoryName(typeof(Program).Assembly.Location) ?? Directory.GetCurrentDirectory();
var configPath = Path.Combine(scriptDirectory, "setup-config.json");

if (!File.Exists(configPath))
{
    // Try relative to the current working directory (running from project root)
    configPath = Path.Combine(Directory.GetCurrentDirectory(), "astrolabe-setup", "setup-config.json");
}

if (!File.Exists(configPath))
{
    // Try current directory (in case we're running from astrolabe-setup folder)
    configPath = Path.Combine(Directory.GetCurrentDirectory(), "setup-config.json");
}

if (!File.Exists(configPath))
{
    Console.Error.WriteLine($"Setup failed: Could not find setup-config.json");
    Console.Error.WriteLine($"Searched in:");
    Console.Error.WriteLine($"  - {Path.Combine(scriptDirectory, "setup-config.json")}");
    Console.Error.WriteLine($"  - {Path.Combine(Directory.GetCurrentDirectory(), "astrolabe-setup", "setup-config.json")}");
    Console.Error.WriteLine($"  - {Path.Combine(Directory.GetCurrentDirectory(), "setup-config.json")}");
    return 1;
}

var configJson = await File.ReadAllTextAsync(configPath);
var config = JsonSerializer.Deserialize(configJson, SetupConfigJsonContext.Default.SetupConfig);

if (config == null)
{
    Console.Error.WriteLine("Setup failed: Could not parse setup-config.json");
    return 1;
}

Console.WriteLine($"Starting setup for project {config.ProjectName}...");
Console.Out.Flush();

var orchestrator = new SetupOrchestrator(config);
try
{
    await orchestrator.RunSetup();

    // Remove setup instructions from README.md to indicate setup is complete
    RemoveSetupInstructionsFromReadme();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Setup failed: {ex.Message}");
    return 1;
}

Console.WriteLine("Setup completed successfully.");

void RemoveSetupInstructionsFromReadme()
{
    // Find project root (go up from astrolabe-setup if needed)
    var currentDir = Directory.GetCurrentDirectory();
    var projectRoot = Path.GetFileName(currentDir) == "astrolabe-setup"
        ? Path.GetDirectoryName(currentDir) ?? currentDir
        : currentDir;

    var readmePath = Path.Combine(projectRoot, "README.md");

    if (!File.Exists(readmePath))
    {
        Console.WriteLine("README.md not found, skipping setup instructions removal.");
        return;
    }

    try
    {
        var content = File.ReadAllText(readmePath);
        const string startMarker = "<!-- SETUP_INSTRUCTIONS_START -->";
        const string endMarker = "<!-- SETUP_INSTRUCTIONS_END -->";

        var startIndex = content.IndexOf(startMarker);
        var endIndex = content.IndexOf(endMarker);

        if (startIndex >= 0 && endIndex > startIndex)
        {
            // Remove the section including markers and any trailing newlines
            var endOfSection = endIndex + endMarker.Length;
            while (endOfSection < content.Length && (content[endOfSection] == '\r' || content[endOfSection] == '\n'))
            {
                endOfSection++;
            }

            content = content.Substring(0, startIndex) + content.Substring(endOfSection);
            File.WriteAllText(readmePath, content);
            Console.WriteLine("✓ Removed setup instructions from README.md");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: Could not update README.md: {ex.Message}");
    }
}

return 0;

// Implicit Program class for top-level statements
partial class Program { }

public class SetupConfig
{
    public string ProjectName { get; set; } = "";
    public string SolutionName { get; set; } = "";
    public int HttpPort { get; set; }
    public int HttpsPort { get; set; }
    public int SpaPort { get; set; }
    public string SiteName { get; set; } = "";
    public bool IncludeDemoData { get; set; }
    public bool IncludeOrleans { get; set; }
}

public class SetupOrchestrator
{
    private readonly SetupConfig _config;
    private readonly string _projectRoot;

    public SetupOrchestrator(SetupConfig config)
    {
        _config = config;
        // If we're running from astrolabe-setup folder, go up one level to project root
        var currentDir = Directory.GetCurrentDirectory();
        _projectRoot = Path.GetFileName(currentDir) == "astrolabe-setup"
            ? Path.GetDirectoryName(currentDir) ?? currentDir
            : currentDir;
    }

    public async Task RunSetup()
    {
        var steps = new List<(string Name, Func<Task> Action)>
        {
            ("Generating secrets", GenerateSecrets),
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
            Console.Out.Flush();
            try
            {
                await action();
                Console.WriteLine($"✓ {name} completed\n");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"X {name} failed: {ex.Message}");
                throw;
            }
        }
    }

    private string ClientAppPath => Path.Combine(_projectRoot, "ClientApp");

    private Task GenerateSecrets()
    {
        var appSettingsPath = Path.Combine(_projectRoot, "appsettings.json");
        if (!File.Exists(appSettingsPath))
        {
            Console.WriteLine("Warning: appsettings.json not found, skipping secret generation.");
            return Task.CompletedTask;
        }

        var content = File.ReadAllText(appSettingsPath);

        // Generate secrets only if placeholders exist
        if (content.Contains("__PasswordSalt__") || content.Contains("__JwtKey__"))
        {
            var passwordSalt = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
            var jwtKey = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));

            content = content.Replace("__PasswordSalt__", passwordSalt);
            content = content.Replace("__JwtKey__", jwtKey);

            File.WriteAllText(appSettingsPath, content);
            Console.WriteLine("Generated secure password salt and JWT key.");
        }
        else
        {
            Console.WriteLine("Secrets already configured, skipping generation.");
        }

        return Task.CompletedTask;
    }

    private async Task BuildBackend()
    {
        await RunCommand("dotnet", "build", _projectRoot);
    }

    private async Task InitializeRush()
    {
        Console.WriteLine("Installing Rush dependencies...");
        await RunCommand("npx", "-y @microsoft/rush@5.153.2 update --bypass-policy", ClientAppPath);
    }

    private async Task GenerateTypeScriptClient()
    {
        var backendProcess = StartBackendProcess();

        try
        {
            await WaitForBackendReady(backendProcess);

            var clientCommonPath = Path.Combine(ClientAppPath, "client-common");
            await RunCommand("npm", "run gencode", clientCommonPath);
            await RunCommand("npm", "run geneditorschemas", clientCommonPath);
        }
        finally
        {
            if (backendProcess != null)
            {
                try
                {
                    // Cancel async output reading before killing process
                    backendProcess.CancelOutputRead();
                    backendProcess.CancelErrorRead();
                }
                catch
                {
                    // Ignore errors during cleanup
                }

                if (!backendProcess.HasExited)
                {
                    backendProcess.Kill(entireProcessTree: true);
                    backendProcess.WaitForExit(5000); // Wait up to 5 seconds for clean exit
                }

                backendProcess.Dispose();
            }
        }
    }

    private async Task InstallFrontendDependencies()
    {
        await RunCommand("npx", "-y @microsoft/rush@5.153.2 update --bypass-policy", ClientAppPath);
    }

    private async Task SeedDatabase()
    {
        Console.WriteLine("Database seeding handled during backend startup.");
        await Task.CompletedTask;
    }

    private Process? StartBackendProcess()
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run",
            WorkingDirectory = _projectRoot,
            UseShellExecute = false,
            CreateNoWindow = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
        };

        var process = Process.Start(processStartInfo);

        if (process != null)
        {
            process.StandardInput.Close();
            // Use BeginOutputReadLine/BeginErrorReadLine instead of Task.Run to avoid hanging
            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null) Console.WriteLine($"[Backend] {e.Data}");
            };
            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null) Console.Error.WriteLine($"[Backend Error] {e.Data}");
            };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        return process;
    }

    private async Task WaitForBackendReady(Process? backendProcess)
    {
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(2);
        var url = $"http://127.0.0.1:{_config.HttpPort}/swagger/v1/swagger.json";

        for (int i = 0; i < 60; i++)
        {
            if (backendProcess != null && backendProcess.HasExited)
            {
                throw new Exception($"Backend process exited unexpectedly with code {backendProcess.ExitCode}");
            }

            try
            {
                var response = await httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Backend is ready!");
                    return;
                }
            }
            catch
            {
                // Backend not ready yet
            }

            if (i % 5 == 0)
            {
                Console.WriteLine("Waiting for backend to be ready...");
                Console.Out.Flush();
            }
            await Task.Delay(1000);
        }
        throw new Exception("Backend failed to start within 60 seconds");
    }

    private async Task RunCommand(string command, string arguments, string workingDirectory)
    {
        var isWindows = OperatingSystem.IsWindows();
        var fileName = command;
        var args = arguments;

        if (isWindows && (command == "npm" || command == "npx" || command == "pnpm"))
        {
            fileName = "cmd.exe";
            args = $"/c {command} {arguments}";
        }

        Console.WriteLine($"Running: {fileName} {args} in {workingDirectory}");

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

        // Use BeginOutputReadLine/BeginErrorReadLine instead of Task.Run to avoid hanging
        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null) Console.WriteLine(e.Data);
        };
        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null) Console.Error.WriteLine(e.Data);
        };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new Exception(
                $"Command failed: {fileName} {args}\nExit code: {process.ExitCode}"
            );
        }
    }
}

[JsonSerializable(typeof(SetupConfig))]
[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString
)]
internal partial class SetupConfigJsonContext : JsonSerializerContext
{
}


