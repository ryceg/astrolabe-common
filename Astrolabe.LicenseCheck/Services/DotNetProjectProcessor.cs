using System.Text.Json;
using Astrolabe.LicenseCheck.Models;
using Spectre.Console;

namespace Astrolabe.LicenseCheck.Services;

public class DotNetProjectProcessor : IProjectProcessor
{
    private readonly ExternalToolRunner _toolRunner;
    private readonly NugetRegistryService _nugetRegistryService;

    public DotNetProjectProcessor(ExternalToolRunner toolRunner)
    {
        _toolRunner = toolRunner;
        _nugetRegistryService = new NugetRegistryService();
    }

    public bool CanProcess(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension == ".sln" || extension == ".csproj";
    }

    public async Task<LicenseReport> ProcessAsync(
        string filePath,
        ReportOptions options,
        LicenseCheckConfig config,
        ProgressContext? context = null
    )
    {
        var outputPath = Path.Combine(options.OutputDirectory, "nuget-licenses.json");

        if (options.Verbose)
        {
            AnsiConsole.MarkupLine($"[grey]Running nuget-license on {filePath}[/]");
        }

        var result = await _toolRunner.RunNugetLicenseAsync(
            filePath,
            options.IncludeTransitive,
            outputPath
        );

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"nuget-license failed with exit code {result.ExitCode}: {result.Error}"
            );
        }

        // Parse the output
        var licenses = await ParseNugetLicenseOutputAsync(outputPath);

        var validator = new LicenseValidatorService(config);
        licenses.ForEach(validator.Validate);

        // Enrich with publish dates
        licenses = await _nugetRegistryService.EnrichWithPublishDatesAsync(
            licenses,
            options.Verbose,
            context
        );

        return new LicenseReport
        {
            ProjectPath = filePath,
            ProjectType = ProjectType.DotNet,
            Licenses = licenses.Where(l => !config.Skiplist.Contains(l.PackageId)).ToList(),
        };
    }

    private async Task<List<LicenseInfo>> ParseNugetLicenseOutputAsync(string outputPath)
    {
        if (!File.Exists(outputPath))
        {
            return new List<LicenseInfo>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(outputPath);
            var nugetLicenses = JsonSerializer.Deserialize<List<NugetLicenseInfo>>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (nugetLicenses == null)
            {
                return new List<LicenseInfo>();
            }

            return nugetLicenses
                .Select(nl => new LicenseInfo
                {
                    PackageId = nl.PackageId,
                    PackageVersion = nl.PackageVersion,
                    License = nl.License,
                    LicenseUrl = nl.LicenseUrl,
                    Authors = nl.Authors,
                    Copyright = nl.Copyright,
                    PackageProjectUrl = nl.PackageProjectUrl,
                    LicenseInformationOrigin = nl.LicenseInformationOrigin,
                })
                .ToList();
        }
        catch (JsonException ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to parse nuget-license output: {ex.Message}[/]");
            return new List<LicenseInfo>();
        }
    }
}
