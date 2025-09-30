using Astrolabe.LicenseCheck.Models;
using Spectre.Console;

namespace Astrolabe.LicenseCheck.Services;

public class NpmProjectProcessor : IProjectProcessor
{
    private readonly NpmLicenseExtractor _licenseExtractor;
    private readonly NpmRegistryService _npmRegistryService;

    public NpmProjectProcessor(ExternalToolRunner toolRunner, string? cacheDirectory = null)
    {
        _licenseExtractor = new NpmLicenseExtractor();
        _npmRegistryService = new NpmRegistryService(cacheDirectory);
    }

    public bool CanProcess(string filePath)
    {
        return Path.GetFileName(filePath)
            .Equals("package.json", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<LicenseReport> ProcessAsync(
        string filePath,
        ReportOptions options,
        LicenseCheckConfig config,
        ProgressContext? context = null
    )
    {
        if (options.Verbose)
        {
            AnsiConsole.MarkupLine($"[grey]Processing npm project: {filePath}[/]");
        }

        var projectDirectory = Path.GetDirectoryName(filePath);
        if (string.IsNullOrEmpty(projectDirectory))
        {
            throw new InvalidOperationException(
                $"Could not determine directory for package.json: {filePath}"
            );
        }

        // Extract license information from node_modules
        var packages = await _licenseExtractor.ExtractFromDirectoryAsync(
            projectDirectory,
            config,
            options
        );

        // Enrich with publish dates
        packages = await _npmRegistryService.EnrichWithPublishDatesAsync(
            packages,
            options.Verbose,
            context
        );

        // Convert to LicenseInfo objects
        var licenses = packages
            .Select(pkg => new LicenseInfo
            {
                PackageId = pkg.Name,
                PackageVersion = pkg.Version,
                License = pkg.License,
                Authors = pkg.Author,
                Repository = pkg.Repository,
                PackageProjectUrl = pkg.Homepage,
                PublishDate = pkg.PublishDate,
                AgeStatus = pkg.AgeStatus,
            })
            .ToList();

        var validator = new LicenseValidatorService(config);
        licenses.ForEach(validator.Validate);

        if (options.Verbose)
        {
            AnsiConsole.MarkupLine($"[grey]Found {licenses.Count} npm packages[/]");
        }

        return new LicenseReport
        {
            ProjectPath = filePath,
            ProjectType = ProjectType.Npm,
            Licenses = licenses,
        };
    }
}
