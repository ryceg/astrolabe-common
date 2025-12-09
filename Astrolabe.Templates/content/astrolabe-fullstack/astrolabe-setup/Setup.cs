using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;

Console.WriteLine("Initializing setup...");
Console.Out.Flush();

// When running with "dotnet run Setup.cs", we need to find setup-config.json
// It will be in the same directory as Setup.cs
var scriptDirectory =
    Path.GetDirectoryName(typeof(Program).Assembly.Location) ?? Directory.GetCurrentDirectory();
var configPath = Path.Combine(scriptDirectory, "setup-config.json");

if (!File.Exists(configPath))
{
    // Try relative to the current working directory (running from project root)
    configPath = Path.Combine(
        Directory.GetCurrentDirectory(),
        "astrolabe-setup",
        "setup-config.json"
    );
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
    Console.Error.WriteLine(
        $"  - {Path.Combine(Directory.GetCurrentDirectory(), "astrolabe-setup", "setup-config.json")}"
    );
    Console.Error.WriteLine(
        $"  - {Path.Combine(Directory.GetCurrentDirectory(), "setup-config.json")}"
    );
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

    // Delete the astrolabe-setup folder
    DeleteSetupFolder();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Setup failed: {ex.Message}");
    return 1;
}

Console.WriteLine("Setup completed successfully.");

void DeleteSetupFolder()
{
    // Find project root and setup folder
    var currentDir = Directory.GetCurrentDirectory();
    var projectRoot =
        Path.GetFileName(currentDir) == "astrolabe-setup"
            ? Path.GetDirectoryName(currentDir) ?? currentDir
            : currentDir;

    var setupFolder = Path.Combine(projectRoot, "astrolabe-setup");

    if (!Directory.Exists(setupFolder))
    {
        return;
    }

    // Change to project root so we're not inside the folder we're deleting
    Directory.SetCurrentDirectory(projectRoot);

    try
    {
        // Try direct deletion first (works on Unix, may work on Windows if files aren't locked)
        Directory.Delete(setupFolder, recursive: true);
        Console.WriteLine("✓ Removed astrolabe-setup folder");
    }
    catch (UnauthorizedAccessException) when (OperatingSystem.IsWindows())
    {
        // On Windows, the running process locks its DLLs - schedule deletion after exit
        ScheduleWindowsDeletion(projectRoot, setupFolder);
    }
    catch (IOException) when (OperatingSystem.IsWindows())
    {
        // File is in use - schedule deletion after exit
        ScheduleWindowsDeletion(projectRoot, setupFolder);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Note: Could not remove setup folder: {ex.Message}");
        Console.WriteLine("You can manually delete the 'astrolabe-setup' folder.");
    }
}

void ScheduleWindowsDeletion(string projectRoot, string setupFolder)
{
    try
    {
        // Use cmd /c start /min to run cleanup in background after this process exits
        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c ping 127.0.0.1 -n 2 >nul & rmdir /s /q \"{setupFolder}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = projectRoot,
        };
        Process.Start(psi);
        Console.WriteLine("✓ Setup folder will be removed shortly");
    }
    catch
    {
        Console.WriteLine("Note: You can manually delete the 'astrolabe-setup' folder.");
    }
}

void RemoveSetupInstructionsFromReadme()
{
    // Find project root (go up from astrolabe-setup if needed)
    var currentDir = Directory.GetCurrentDirectory();
    var projectRoot =
        Path.GetFileName(currentDir) == "astrolabe-setup"
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
            while (
                endOfSection < content.Length
                && (content[endOfSection] == '\r' || content[endOfSection] == '\n')
            )
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
        _projectRoot =
            Path.GetFileName(currentDir) == "astrolabe-setup"
                ? Path.GetDirectoryName(currentDir) ?? currentDir
                : currentDir;
    }

    public async Task RunSetup()
    {
        var steps = new List<(string Name, Func<Task> Action)>
        {
            ("Generating secrets", GenerateSecrets),
            ("Building backend", BuildBackend),
            ("Fetching Astrolabe UI components", FetchAstrolabeUI),
            ("Initializing Rush", InitializeRush),
            ("Generating TypeScript client", GenerateTypeScriptClient),
            ("Installing frontend dependencies", InstallFrontendDependencies),
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
            var passwordSalt = Convert.ToBase64String(
                System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)
            );
            var jwtKey = Convert.ToBase64String(
                System.Security.Cryptography.RandomNumberGenerator.GetBytes(64)
            );

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

    private async Task FetchAstrolabeUI()
    {
        const string repoOwner = "astrolabe-apps";
        const string repoName = "astrolabe-common";
        const string branch = "main";
        const string sourcePath = "astrolabe-ui";

        var astrolabeUiPath = Path.Combine(ClientAppPath, "astrolabe-ui");
        var srcPath = Path.Combine(astrolabeUiPath, "src");
        var tempDir = Path.Combine(Path.GetTempPath(), $"astrolabe-ui-{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(tempDir);

            // Download tarball from GitHub
            var tarballUrl = $"https://github.com/{repoOwner}/{repoName}/archive/refs/heads/{branch}.tar.gz";
            var tarballPath = Path.Combine(tempDir, "repo.tar.gz");

            Console.WriteLine($"Downloading astrolabe-ui from {repoOwner}/{repoName}...");

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", "astrolabe-setup");
                var response = await httpClient.GetAsync(tarballUrl);
                response.EnsureSuccessStatusCode();
                await using var fs = File.Create(tarballPath);
                await response.Content.CopyToAsync(fs);
            }

            // Extract tarball using .NET native APIs (cross-platform)
            Console.WriteLine("Extracting components...");
            await ExtractTarGzAsync(tarballPath, tempDir);

            // Find the extracted folder (it will be named like astrolabe-common-main)
            var extractedDir = Directory.GetDirectories(tempDir)
                .FirstOrDefault(d => Path.GetFileName(d).StartsWith($"{repoName}-"));

            if (extractedDir == null)
            {
                throw new Exception("Failed to find extracted repository folder");
            }

            var sourceUiPath = Path.Combine(extractedDir, sourcePath);

            if (!Directory.Exists(sourceUiPath))
            {
                throw new Exception($"Source path {sourcePath} not found in repository");
            }


            // Copy entire astrolabe-ui folder (src, package.json, tsconfig.json)
            Console.WriteLine("Copying astrolabe-ui components...");
            CopyDirectoryContents(sourceUiPath, astrolabeUiPath, preserveExisting: false);



            Console.WriteLine($"Fetched {CountFiles(srcPath)} component files from upstream.");
        }
        finally
        {
            // Cleanup temp directory
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    private static async Task ExtractTarGzAsync(string tarGzPath, string destinationDir)
    {
        // Open the .tar.gz file and decompress the gzip layer
        await using var fileStream = File.OpenRead(tarGzPath);
        await using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);

        // Extract tar entries
        await TarFile.ExtractToDirectoryAsync(gzipStream, destinationDir, overwriteFiles: true);
    }

    private void CopyDirectoryContents(string sourceDir, string targetDir, bool preserveExisting)
    {
        Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(file);
            var targetPath = Path.Combine(targetDir, fileName);

            // Skip if file exists and we want to preserve existing
            if (preserveExisting && File.Exists(targetPath))
            {
                Console.WriteLine($"  Keeping existing: {fileName}");
                continue;
            }

            File.Copy(file, targetPath, overwrite: true);
            Console.WriteLine($"  Copied: {fileName}");
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var dirName = Path.GetFileName(dir);
            CopyDirectoryContents(dir, Path.Combine(targetDir, dirName), preserveExisting);
        }
    }



    private int CountFiles(string directory)
    {
        return Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories).Length;
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
                if (e.Data != null)
                    Console.WriteLine($"[Backend] {e.Data}");
            };
            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    Console.Error.WriteLine($"[Backend Error] {e.Data}");
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
                throw new Exception(
                    $"Backend process exited unexpectedly with code {backendProcess.ExitCode}"
                );
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
internal partial class SetupConfigJsonContext : JsonSerializerContext { }
