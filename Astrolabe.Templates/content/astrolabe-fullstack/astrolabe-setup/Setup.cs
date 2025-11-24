using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

Console.WriteLine("Initializing setup...");
Console.Out.Flush();

var configPath = Path.Combine(AppContext.BaseDirectory, "setup-config.json");

if (!File.Exists(configPath))
{
    configPath = Path.Combine(Directory.GetCurrentDirectory(), "astrolabe-setup", "setup-config.json");
}

if (!File.Exists(configPath))
{
    Console.Error.WriteLine($"Setup failed: Could not find file '{configPath}'.");
    return 1;
}

var configJson = await File.ReadAllTextAsync(configPath);
var jsonOptions = new JsonSerializerOptions
{
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    PropertyNameCaseInsensitive = true
};
var config = JsonSerializer.Deserialize<SetupConfig>(configJson, jsonOptions);

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
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Setup failed: {ex.Message}");
    return 1;
}

Console.WriteLine("Setup completed successfully.");
return 0;

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

public class SetupOrchestrator
{
    private readonly SetupConfig _config;
    private readonly string _projectRoot;

    public SetupOrchestrator(SetupConfig config)
    {
        _config = config;
        _projectRoot = Directory.GetCurrentDirectory();
    }

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
            if (backendProcess != null && !backendProcess.HasExited)
            {
                backendProcess.Kill(entireProcessTree: true);
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
        };

        var process = Process.Start(processStartInfo);

        if (process != null)
        {
             Task.Run(async () =>
            {
                while (!process.StandardOutput.EndOfStream)
                {
                    var line = await process.StandardOutput.ReadLineAsync();
                    if (line != null) Console.WriteLine($"[Backend] {line}");
                }
            });

            Task.Run(async () =>
            {
                while (!process.StandardError.EndOfStream)
                {
                    var line = await process.StandardError.ReadLineAsync();
                    if (line != null) Console.Error.WriteLine($"[Backend Error] {line}");
                }
            });
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
                    Console.WriteLine($"Connected to backend at {url}");
                    return;
                }
                else
                {
                    if (i % 5 == 0) Console.WriteLine($"Backend returned {response.StatusCode} at {url}");
                }
            }
            catch (Exception ex)
            {
                if (i % 5 == 0) Console.WriteLine($"Failed to connect to backend: {ex.Message}");
            }

            if (i % 5 == 0) {
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
            UseShellExecute = false,
            CreateNoWindow = false,
        };

        using var process = Process.Start(processStartInfo);
        if (process == null)
            throw new Exception($"Failed to start process: {fileName} {args}");

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        var outputTask = Task.Run(async () =>
        {
            while (!process.StandardOutput.EndOfStream)
            {
                var line = await process.StandardOutput.ReadLineAsync();
                if (line != null)
                {
                    Console.WriteLine(line);
                    outputBuilder.AppendLine(line);
                }
            }
        });

        var errorTask = Task.Run(async () =>
        {
            while (!process.StandardError.EndOfStream)
            {
                var line = await process.StandardError.ReadLineAsync();
                if (line != null)
                {
                    Console.Error.WriteLine(line);
                    errorBuilder.AppendLine(line);
                }
            }
        });

        await Task.WhenAll(outputTask, errorTask);
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new Exception(
                $"Command failed: {fileName} {args}\nExit code: {process.ExitCode}"
            );
        }
    }
}
