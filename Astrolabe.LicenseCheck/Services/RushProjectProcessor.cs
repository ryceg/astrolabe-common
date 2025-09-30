using System.Text.Json;
using Astrolabe.LicenseCheck.Models;
using Spectre.Console;

namespace Astrolabe.LicenseCheck.Services;

public class RushProjectProcessor : IProjectProcessor
{
    private readonly NpmLicenseExtractor _licenseExtractor;
    private readonly NpmRegistryService _npmRegistryService;

    public RushProjectProcessor(ExternalToolRunner toolRunner, string? cacheDirectory = null)
    {
        _licenseExtractor = new NpmLicenseExtractor();
        _npmRegistryService = new NpmRegistryService(cacheDirectory);
    }

    public bool CanProcess(string filePath)
    {
        return Path.GetFileName(filePath).Equals("rush.json", StringComparison.OrdinalIgnoreCase);
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
            AnsiConsole.MarkupLine($"[grey]Processing Rush project: {filePath}[/]");
        }

        var rushDirectory = Path.GetDirectoryName(filePath);
        if (string.IsNullOrEmpty(rushDirectory))
        {
            throw new InvalidOperationException(
                $"Could not determine directory for rush.json: {filePath}"
            );
        }

        // Parse rush.json to get project folders
        var projects = await ParseRushConfigurationAsync(filePath, rushDirectory);
        var allPackages = new List<PackageMetadata>();

        if (options.Verbose)
        {
            AnsiConsole.MarkupLine($"[grey]Found {projects.Count} Rush projects to process[/]");
        }

        // Process each project in the Rush monorepo
        foreach (var project in projects)
        {
            try
            {
                if (options.Verbose)
                {
                    AnsiConsole.MarkupLine($"[grey]  Processing {project.PackageName}...[/]");
                }

                var packages = await _licenseExtractor.ExtractFromDirectoryAsync(
                    project.ProjectPath,
                    config,
                    options
                );

                allPackages.AddRange(packages);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine(
                    $"[yellow]Warning: Could not process Rush project {project.PackageName}: {ex.Message}[/]"
                );
            }
        }

        // Remove duplicates based on package name and version
        var uniquePackages = allPackages
            .GroupBy(l => $"{l.Name}@{l.Version}")
            .Select(g => g.First())
            .ToList();

        // Enrich with publish dates
        uniquePackages = await _npmRegistryService.EnrichWithPublishDatesAsync(
            uniquePackages,
            options.Verbose,
            context
        );

        var uniqueLicenses = uniquePackages
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
        uniqueLicenses.ForEach(validator.Validate);

        if (options.Verbose)
        {
            AnsiConsole.MarkupLine(
                $"[grey]Found {uniqueLicenses.Count} unique npm packages across all Rush projects[/]"
            );
        }

        return new LicenseReport
        {
            ProjectPath = filePath,
            ProjectType = ProjectType.Rush,
            Licenses = uniqueLicenses,
        };
    }

    private async Task<List<RushProject>> ParseRushConfigurationAsync(
        string rushJsonPath,
        string rushDirectory
    )
    {
        var projects = new List<RushProject>();

        try
        {
            var json = await File.ReadAllTextAsync(rushJsonPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            };

            using var document = JsonDocument.Parse(
                json,
                new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                }
            );

            if (document.RootElement.TryGetProperty("projects", out var projectsElement))
            {
                foreach (var projectElement in projectsElement.EnumerateArray())
                {
                    if (
                        projectElement.TryGetProperty("packageName", out var packageNameProperty)
                        && projectElement.TryGetProperty(
                            "projectFolder",
                            out var projectFolderProperty
                        )
                    )
                    {
                        var packageName = packageNameProperty.GetString();
                        var projectFolder = projectFolderProperty.GetString();

                        if (
                            !string.IsNullOrEmpty(packageName)
                            && !string.IsNullOrEmpty(projectFolder)
                        )
                        {
                            var fullProjectPath = Path.IsPathRooted(projectFolder)
                                ? projectFolder
                                : Path.GetFullPath(Path.Combine(rushDirectory, projectFolder));

                            projects.Add(
                                new RushProject
                                {
                                    PackageName = packageName,
                                    ProjectFolder = projectFolder,
                                    ProjectPath = fullProjectPath,
                                }
                            );
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to parse rush.json: {ex.Message}[/]");
        }

        return projects;
    }

    private class RushProject
    {
        public string PackageName { get; set; } = string.Empty;
        public string ProjectFolder { get; set; } = string.Empty;
        public string ProjectPath { get; set; } = string.Empty;
    }
}
