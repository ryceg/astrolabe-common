using System.Text.Json;
using Astrolabe.LicenseCheck.Models;
using ClosedXML.Excel;
using Spectre.Console;

namespace Astrolabe.LicenseCheck.Outputs;

public class ExcelOutputGenerator : IOutputGenerator
{
    public async Task GenerateAsync(List<LicenseReport> reports, ReportOptions options)
    {
        var outputPath = Path.Combine(options.OutputDirectory, "license-report.xlsx");

        using var workbook = new XLWorkbook();

        // Add NuGet packages sheet
        await AddNuGetPackagesSheetAsync(workbook, reports);

        // Add NPM dependencies sheet
        await AddNpmDependenciesSheetAsync(workbook, reports);

        // Only save if we have at least one sheet with data
        if (workbook.Worksheets.Any())
        {
            workbook.SaveAs(outputPath);

            if (options.Verbose)
            {
                AnsiConsole.MarkupLine($"[green]✓ Excel report saved to {outputPath}[/]");
            }
        }
    }

    private async Task AddNuGetPackagesSheetAsync(XLWorkbook workbook, List<LicenseReport> reports)
    {
        var licenses = reports
            .Where(r => r.ProjectType == ProjectType.DotNet)
            .SelectMany(r => r.Licenses)
            .ToList();

        if (licenses.Any())
        {
            try
            {
                var worksheet = workbook.Worksheets.Add("NuGet Packages");

                // Headers
                worksheet.Cell(1, 1).Value = "Package ID";
                worksheet.Cell(1, 2).Value = "Version";
                worksheet.Cell(1, 3).Value = "License";
                worksheet.Cell(1, 4).Value = "License URL";
                worksheet.Cell(1, 5).Value = "Authors";
                worksheet.Cell(1, 6).Value = "Copyright";
                worksheet.Cell(1, 7).Value = "Project URL";
                worksheet.Cell(1, 8).Value = "Publish Date";
                worksheet.Cell(1, 9).Value = "Age Status";
                worksheet.Cell(1, 10).Value = "Is Problematic";
                worksheet.Cell(1, 11).Value = "Is Safelisted";
                worksheet.Cell(1, 12).Value = "Problem Reason";

                // Format headers
                var headerRange = worksheet.Range(1, 1, 1, 12);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

                // Data
                for (int i = 0; i < licenses.Count; i++)
                {
                    var license = licenses[i];
                    var row = i + 2;

                    worksheet.Cell(row, 1).Value = license.PackageId;
                    worksheet.Cell(row, 2).Value = license.PackageVersion;
                    worksheet.Cell(row, 3).Value = license.License ?? "";
                    worksheet.Cell(row, 4).Value = license.LicenseUrl ?? "";
                    worksheet.Cell(row, 5).Value = license.Authors ?? "";
                    worksheet.Cell(row, 6).Value = license.Copyright ?? "";
                    worksheet.Cell(row, 7).Value = license.PackageProjectUrl ?? "";
                    worksheet.Cell(row, 8).Value =
                        license.PublishDate?.ToString("yyyy-MM-dd") ?? "";
                    worksheet.Cell(row, 9).Value = license.AgeStatus.ToString();
                    worksheet.Cell(row, 10).Value = license.IsProblematic;
                    worksheet.Cell(row, 11).Value = license.IsSafelisted;
                    worksheet.Cell(row, 12).Value = license.ProblemReason ?? "";
                }

                // Auto-fit columns
                worksheet.Columns().AdjustToContents();
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine(
                    $"[red]Failed to process NuGet licenses for Excel: {ex.Message}[/]"
                );
            }
        }
    }

    private async Task AddNpmDependenciesSheetAsync(XLWorkbook workbook, List<LicenseReport> reports)
    {
        var licenses = reports
            .Where(r => r.ProjectType == ProjectType.Npm || r.ProjectType == ProjectType.Rush)
            .SelectMany(r => r.Licenses)
            .ToList();

        if (licenses.Any())
        {
            try
            {
                var worksheet = workbook.Worksheets.Add("NPM Dependencies");

                // Headers
                worksheet.Cell(1, 1).Value = "Package Name";
                worksheet.Cell(1, 2).Value = "Version";
                worksheet.Cell(1, 3).Value = "License";
                worksheet.Cell(1, 4).Value = "Repository";
                worksheet.Cell(1, 5).Value = "Publish Date";
                worksheet.Cell(1, 6).Value = "Age Status";
                worksheet.Cell(1, 7).Value = "Is Problematic";
                worksheet.Cell(1, 8).Value = "Is Safelisted";
                worksheet.Cell(1, 9).Value = "Problem Reason";

                // Format headers
                var headerRange = worksheet.Range(1, 1, 1, 9);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGreen;

                // Data
                for (int i = 0; i < licenses.Count; i++)
                {
                    var license = licenses[i];
                    var row = i + 2;

                    worksheet.Cell(row, 1).Value = license.PackageId;
                    worksheet.Cell(row, 2).Value = license.PackageVersion;
                    worksheet.Cell(row, 3).Value = license.License ?? "";
                    worksheet.Cell(row, 4).Value = license.Repository ?? "";
                    worksheet.Cell(row, 5).Value = license.PublishDate?.ToString("yyyy-MM-dd") ?? "";
                    worksheet.Cell(row, 6).Value = license.AgeStatus.ToString();
                    worksheet.Cell(row, 7).Value = license.IsProblematic;
                    worksheet.Cell(row, 8).Value = license.IsSafelisted;
                    worksheet.Cell(row, 9).Value = license.ProblemReason ?? "";
                }

                // Auto-fit columns
                worksheet.Columns().AdjustToContents();
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine(
                    $"[red]Failed to process NPM dependencies for Excel: {ex.Message}[/]"
                );
            }
        }
    }

}
