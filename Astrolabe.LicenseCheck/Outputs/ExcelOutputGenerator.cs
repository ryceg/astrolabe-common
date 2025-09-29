using System.Text.Json;
using Astrolabe.LicenseCheck.Models;
using ClosedXML.Excel;
using Spectre.Console;

namespace Astrolabe.LicenseCheck.Outputs;

public class ExcelOutputGenerator : IOutputGenerator
{
    public bool SupportsFormat(OutputFormat format)
    {
        return format == OutputFormat.Excel || format == OutputFormat.All;
    }

    public async Task GenerateAsync(List<LicenseReport> reports, ReportOptions options)
    {
        var outputPath = Path.Combine(options.OutputDirectory, "license-report.xlsx");

        using var workbook = new XLWorkbook();

        // Add NuGet packages sheet
        await AddNuGetPackagesSheetAsync(workbook, options.OutputDirectory);

        // Add Rush dependencies sheet
        await AddRushDependenciesSheetAsync(workbook, options.OutputDirectory);

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

    private async Task AddNuGetPackagesSheetAsync(XLWorkbook workbook, string outputDirectory)
    {
        var nugetJsonPath = Path.Combine(outputDirectory, "nuget-licenses.json");

        if (File.Exists(nugetJsonPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(nugetJsonPath);
                var licenses = JsonSerializer.Deserialize<List<LicenseInfo>>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (licenses?.Any() == true)
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
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine(
                    $"[red]Failed to process NuGet licenses for Excel: {ex.Message}[/]"
                );
            }
        }
    }

    private async Task AddRushDependenciesSheetAsync(XLWorkbook workbook, string outputDirectory)
    {
        var csvPath = Path.Combine(outputDirectory, "rush-dependencies.csv");

        if (File.Exists(csvPath))
        {
            try
            {
                var lines = await File.ReadAllLinesAsync(csvPath);
                if (lines.Length > 1) // Has data beyond header
                {
                    var worksheet = workbook.Worksheets.Add("Rush Dependencies");

                    for (int i = 0; i < lines.Length; i++)
                    {
                        var parts = ParseCsvLine(lines[i]);
                        for (int j = 0; j < parts.Length; j++)
                        {
                            worksheet.Cell(i + 1, j + 1).Value = parts[j];
                        }
                    }

                    // Format headers
                    if (lines.Length > 0)
                    {
                        var parts = ParseCsvLine(lines[0]);
                        var headerRange = worksheet.Range(1, 1, 1, parts.Length);
                        headerRange.Style.Font.Bold = true;
                        headerRange.Style.Fill.BackgroundColor = XLColor.LightGreen;
                    }

                    // Auto-fit columns
                    worksheet.Columns().AdjustToContents();
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine(
                    $"[red]Failed to process Rush dependencies for Excel: {ex.Message}[/]"
                );
            }
        }
    }

    private string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    // Escaped quote
                    current.Append('"');
                    i++; // Skip next quote
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        result.Add(current.ToString());
        return result.ToArray();
    }
}
