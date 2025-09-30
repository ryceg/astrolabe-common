using System.Text;
using Astrolabe.LicenseCheck.Models;
using Spectre.Console;

namespace Astrolabe.LicenseCheck.Outputs;

public class CsvOutputGenerator : IOutputGenerator
{
    public async Task GenerateAsync(List<LicenseReport> reports, ReportOptions options)
    {
        // Generate .NET CSV
        var dotNetReports = reports.Where(r => r.ProjectType == ProjectType.DotNet).ToList();
        if (dotNetReports.Any())
        {
            var outputPath = Path.Combine(options.OutputDirectory, "nuget-licenses.csv");
            var csv = new StringBuilder();

            // CSV header
            csv.AppendLine(
                "PackageId,Version,License,LicenseUrl,Authors,Copyright,ProjectUrl,PublishDate,AgeStatus,IsProblematic,IsSafelisted,ProblemReason"
            );

            foreach (var report in dotNetReports)
            {
                foreach (var license in report.Licenses)
                {
                    var row =
                        $"{EscapeCsv(license.PackageId)},{EscapeCsv(license.PackageVersion)},{EscapeCsv(license.License ?? "Unknown")},{EscapeCsv(license.LicenseUrl ?? "")},{EscapeCsv(license.Authors ?? "")},{EscapeCsv(license.Copyright ?? "")},{EscapeCsv(license.PackageProjectUrl ?? "")},{EscapeCsv(license.PublishDate?.ToString("yyyy-MM-dd") ?? "")},{EscapeCsv(license.AgeStatus.ToString())},{license.IsProblematic},{license.IsSafelisted},{EscapeCsv(license.ProblemReason ?? "")}";
                    csv.AppendLine(row);
                }
            }

            await File.WriteAllTextAsync(outputPath, csv.ToString());

            if (options.Verbose)
            {
                AnsiConsole.MarkupLine($"[green]✓ CSV report saved to {outputPath}[/]");
            }
        }

        // Generate NPM/Rush CSV
        var npmReports = reports
            .Where(r => r.ProjectType == ProjectType.Npm || r.ProjectType == ProjectType.Rush)
            .ToList();

        if (npmReports.Any())
        {
            var outputPath = Path.Combine(options.OutputDirectory, "npm-dependencies.csv");
            var csv = new StringBuilder();

            // CSV header
            csv.AppendLine(
                "PackageName,Version,License,Repository,PublishDate,AgeStatus,IsProblematic,IsSafelisted,ProblemReason"
            );

            foreach (var report in npmReports)
            {
                foreach (var license in report.Licenses)
                {
                    var row =
                        $"{EscapeCsv(license.PackageId)},{EscapeCsv(license.PackageVersion)},{EscapeCsv(license.License ?? "Unknown")},{EscapeCsv(license.Repository ?? "Unknown")},{EscapeCsv(license.PublishDate?.ToString("yyyy-MM-dd") ?? "")},{EscapeCsv(license.AgeStatus.ToString())},{license.IsProblematic},{license.IsSafelisted},{EscapeCsv(license.ProblemReason ?? "")}";
                    csv.AppendLine(row);
                }
            }

            await File.WriteAllTextAsync(outputPath, csv.ToString());

            if (options.Verbose)
            {
                AnsiConsole.MarkupLine($"[green]✓ CSV report saved to {outputPath}[/]");
            }
        }
    }

    private string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
