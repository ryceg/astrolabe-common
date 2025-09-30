using System.Text.Json;
using Astrolabe.LicenseCheck.Models;
using Spectre.Console;

namespace Astrolabe.LicenseCheck.Outputs;

public class JsonOutputGenerator : IOutputGenerator
{
    public async Task GenerateAsync(List<LicenseReport> reports, ReportOptions options)
    {
        var dotNetReports = reports.Where(r => r.ProjectType == ProjectType.DotNet).ToList();

        if (dotNetReports.Any())
        {
            var allLicenses = dotNetReports.SelectMany(r => r.Licenses).ToList();
            var outputPath = Path.Combine(options.OutputDirectory, "nuget-licenses.json");

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };

            var json = JsonSerializer.Serialize(allLicenses, jsonOptions);
            await File.WriteAllTextAsync(outputPath, json);

            if (options.Verbose)
            {
                AnsiConsole.MarkupLine($"[green]✓ JSON report saved to {outputPath}[/]");
            }
        }
    }
}
