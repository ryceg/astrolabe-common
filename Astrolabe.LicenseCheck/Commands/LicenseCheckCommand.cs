using System.ComponentModel;
using Astrolabe.LicenseCheck.Models;
using Astrolabe.LicenseCheck.Outputs;
using Astrolabe.LicenseCheck.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace Astrolabe.LicenseCheck.Commands;

[Description("Checks licenses for .NET and npm projects and generates a report with package age analysis.")]
public class LicenseCheckCommand : Command<LicenseCheckCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[files]")]
        [Description(
            "Input files (.sln, .csproj, rush.json, package.json). If not specified, searches for .sln files in current directory."
        )]
        public string[]? InputFiles { get; set; }

        [CommandOption("-o|--output-dir")]
        [Description("Output directory for license reports")]
        [DefaultValue("./license-reports")]
        public string OutputDirectory { get; set; } = "./license-reports";

        [CommandOption("-t|--include-transitive")]
        [Description("Include transitive dependencies")]
        public bool IncludeTransitive { get; set; }

        [CommandOption("--format")]
        [Description("Output format")]
        [DefaultValue(OutputFormat.All)]
        public OutputFormat Format { get; set; } = OutputFormat.All;

        [CommandOption("-v|--verbose")]
        [Description("Enable verbose output")]
        public bool Verbose { get; set; }

        [CommandOption("--exclude-private-packages")]
        [Description("Exclude private packages (for npm/Rush)")]
        public bool ExcludePrivatePackages { get; set; }

        [CommandOption("--production")]
        [Description("Only include production dependencies (for npm/Rush)")]
        public bool ProductionOnly { get; set; }

        [CommandOption("--development")]
        [Description("Only include development dependencies (for npm/Rush)")]
        public bool DevelopmentOnly { get; set; }

        [CommandOption("--nested-search-path")]
        [Description("A glob pattern to use for discovering nested package.json files (e.g., '**/ClientApp/**'). Can be specified multiple times.")]
        public string[]? NestedSearchPaths { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        return ExecuteAsync(context, settings).GetAwaiter().GetResult();
    }

    private async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            AnsiConsole.Write(new FigletText("License Check").LeftJustified().Color(Color.Blue));

            var options = new ReportOptions
            {
                OutputDirectory = Path.GetFullPath(settings.OutputDirectory),
                IncludeTransitive = settings.IncludeTransitive,
                Format = settings.Format,
                Verbose = settings.Verbose,
                ExcludePrivatePackages = settings.ExcludePrivatePackages,
                ProductionOnly = settings.ProductionOnly,
                DevelopmentOnly = settings.DevelopmentOnly
            };

            if (settings.ProductionOnly && settings.DevelopmentOnly)
            {
                throw new ArgumentException("Cannot specify both --production and --development.");
            }

            // Create output directory
            Directory.CreateDirectory(options.OutputDirectory);

            // Determine input files
            var inputFiles = await GetInputFilesAsync(settings);
            if (!inputFiles.Any())
            {
                AnsiConsole.MarkupLine(
                    "[red]No input files found. Specify files or ensure .sln files exist in current directory.[/]"
                );
                return 1;
            }

            AnsiConsole.MarkupLine($"[green]Output directory:[/] {options.OutputDirectory}");
            AnsiConsole.MarkupLine($"[green]Found {inputFiles.Count} input file(s):[/]");
            foreach (var file in inputFiles)
            {
                AnsiConsole.MarkupLine($"  • {file}");
            }

            // Check external tool availability
            var toolRunner = new ExternalToolRunner();
            await VerifyToolsAsync(toolRunner);

            // Load configuration
            var configService = new ConfigurationService();
            var config = configService.LoadConfig(Environment.CurrentDirectory);

            // Process files
            var processors = CreateProcessors(toolRunner);
            var reports = new List<LicenseReport>();

            await AnsiConsole
                .Progress()
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask(
                        "[green]Processing files[/]",
                        maxValue: inputFiles.Count
                    );

                    foreach (var file in inputFiles)
                    {
                        task.Description = $"[green]Processing[/] {Path.GetFileName(file)}";

                        var processor = GetProcessorForFile(file, processors);
                        if (processor != null)
                        {
                            var report = await processor.ProcessAsync(file, options, config, ctx);
                            reports.Add(report);
                        }

                        task.Increment(1);
                    }
                });

            // Generate outputs
            await GenerateOutputsAsync(reports, options);

            var problematicPackages = reports
                .SelectMany(r => r.Licenses)
                .Where(l => l.IsProblematic && !l.IsSafelisted)
                .ToList();

            if (problematicPackages.Any())
            {
                AnsiConsole.MarkupLine("[red]Found packages with problematic licenses that are not safelisted:[/]");

                var table = new Table();
                table.AddColumn("Package ID");
                table.AddColumn("Version");
                table.AddColumn("License");
                table.AddColumn("Reason");

                foreach (var pkg in problematicPackages)
                {
                    table.AddRow(pkg.PackageId, pkg.PackageVersion, pkg.License ?? "N/A", pkg.ProblemReason ?? "N/A");
                }

                AnsiConsole.Write(table);

                return 2;
            }

            AnsiConsole.MarkupLine("[green]✓ License checking completed successfully![/]");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }

    private async Task<List<string>> GetInputFilesAsync(Settings settings)
    {
        var filesToProcess = new HashSet<string>();

        var initialPaths = settings.InputFiles?.Any() == true ? settings.InputFiles.ToList() : new List<string> { "." };

        foreach (var path in initialPaths)
        {
            if (File.Exists(path))
            {
                filesToProcess.Add(Path.GetFullPath(path));
            }
            else if (Directory.Exists(path))
            {
                var slnFiles = Directory.GetFiles(path, "*.sln", SearchOption.TopDirectoryOnly);
                if (slnFiles.Any())
                {
                    foreach (var file in slnFiles) filesToProcess.Add(Path.GetFullPath(file));
                }
                else
                {
                    var csprojFiles = Directory.GetFiles(path, "*.csproj", SearchOption.AllDirectories);
                    foreach (var file in csprojFiles) filesToProcess.Add(Path.GetFullPath(file));
                }
            }
        }

        var matcher = new Matcher();
        matcher.AddExclude("**/node_modules/**");

        var searchPatterns = new List<string>();
        if (settings.NestedSearchPaths?.Any() == true)
        {
            searchPatterns.AddRange(settings.NestedSearchPaths);
        }
        else
        {
            searchPatterns.Add(Path.Combine("**", "ClientApp", "sites", "**", "package.json"));
        }

        foreach (var pattern in searchPatterns)
        {
            matcher.AddInclude(pattern);
        }

        var result = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(".")));
        var matchedFiles = result.Files.Select(f => Path.GetFullPath(f.Path));

        foreach (var file in matchedFiles)
        {
            filesToProcess.Add(file);
        }

        return filesToProcess.ToList();
    }

    private async Task VerifyToolsAsync(ExternalToolRunner toolRunner)
    {
        AnsiConsole.MarkupLine("[yellow]Checking external tools...[/]");

        var nugetLicenseAvailable = await toolRunner.IsNugetLicenseAvailableAsync();

        if (!nugetLicenseAvailable)
        {
            AnsiConsole.MarkupLine("[red]⚠ nuget-license not found.[/]");
            AnsiConsole.MarkupLine(
                "[grey]Install with: dotnet tool install --global nuget-license[/]"
            );
            throw new InvalidOperationException(
                "nuget-license is required for .NET project processing. Please install it and try again."
            );
        }

        AnsiConsole.MarkupLine("[green]✓ nuget-license is available[/]");
    }

    private List<IProjectProcessor> CreateProcessors(ExternalToolRunner toolRunner)
    {
        return new List<IProjectProcessor>
        {
            new DotNetProjectProcessor(toolRunner),
            new RushProjectProcessor(toolRunner),
            new NpmProjectProcessor(toolRunner),
        };
    }

    private IProjectProcessor? GetProcessorForFile(string file, List<IProjectProcessor> processors)
    {
        return processors.FirstOrDefault(p => p.CanProcess(file));
    }

    private async Task GenerateOutputsAsync(List<LicenseReport> reports, ReportOptions options)
    {
        var generators = new List<IOutputGenerator>
        {
            new JsonOutputGenerator(),
            new CsvOutputGenerator(),
            new ExcelOutputGenerator(),
        };

        foreach (var generator in generators)
        {
            if (generator.SupportsFormat(options.Format))
            {
                await generator.GenerateAsync(reports, options);
            }
        }
    }
}

