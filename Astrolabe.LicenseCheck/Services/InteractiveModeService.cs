using System.Text.Json;
using Astrolabe.LicenseCheck.Models;
using Spectre.Console;

namespace Astrolabe.LicenseCheck.Services;

public class InteractiveModeService
{
    private readonly ConfigurationService _configService;
    private LicenseCheckConfig _config;
    private readonly List<ConfigChange> _changes = new();

    public InteractiveModeService(LicenseCheckConfig config, ConfigurationService configService)
    {
        _config = config;
        _configService = configService;
    }

    public async Task<InteractiveModeResult> ReviewProblematicPackagesAsync(
        List<LicenseInfo> problematicPackages
    )
    {
        if (!problematicPackages.Any())
        {
            return new InteractiveModeResult { ConfigUpdated = false };
        }

        AnsiConsole.MarkupLine(
            $"\n[yellow]Found {problematicPackages.Count} package(s) with problematic licenses.[/]"
        );

        var shouldReview = AnsiConsole.Confirm(
            "Would you like to review them interactively?",
            defaultValue: true
        );

        if (!shouldReview)
        {
            return new InteractiveModeResult { ConfigUpdated = false };
        }

        AnsiConsole.WriteLine();

        foreach (var package in problematicPackages)
        {
            await ReviewPackageAsync(package);
        }

        if (!_changes.Any())
        {
            AnsiConsole.MarkupLine("[grey]No changes made.[/]");
            return new InteractiveModeResult { ConfigUpdated = false };
        }

        ShowChangesSummary();

        var shouldSave = AnsiConsole.Confirm(
            "\nSave these changes to license-check.json?",
            defaultValue: true
        );

        if (shouldSave)
        {
            await SaveConfigurationAsync();
            AnsiConsole.MarkupLine("[green]✓ Configuration saved to license-check.json[/]");

            var shouldRerun = AnsiConsole.Confirm(
                "Re-run license check with new configuration?",
                defaultValue: true
            );

            return new InteractiveModeResult { ConfigUpdated = true, ShouldRerun = shouldRerun };
        }

        AnsiConsole.MarkupLine("[yellow]Changes discarded.[/]");
        return new InteractiveModeResult { ConfigUpdated = false };
    }

    private async Task ReviewPackageAsync(LicenseInfo package)
    {
        DisplayPackageDetails(package);

        var action = AnsiConsole.Prompt(
            new SelectionPrompt<PackageAction>()
                .Title("What would you like to do?")
                .AddChoices(
                    PackageAction.SafelistPackage,
                    PackageAction.AllowLicense,
                    PackageAction.DisallowLicense,
                    PackageAction.SkipPackage,
                    PackageAction.ViewDetails,
                    PackageAction.SkipDecision,
                    PackageAction.Quit
                )
                .UseConverter(action => action switch
                {
                    PackageAction.SafelistPackage => "Safelist this package (won't fail build)",
                    PackageAction.AllowLicense => $"Allow '{package.License}' license globally",
                    PackageAction.DisallowLicense =>
                        $"Disallow '{package.License}' license globally",
                    PackageAction.SkipPackage => "Skip this package in future scans",
                    PackageAction.ViewDetails => "View more details",
                    PackageAction.SkipDecision => "Skip decision (keep as problematic)",
                    PackageAction.Quit => "Quit interactive mode",
                    _ => "Unknown",
                })
        );

        switch (action)
        {
            case PackageAction.SafelistPackage:
                await SafelistPackageAsync(package);
                break;
            case PackageAction.AllowLicense:
                AllowLicense(package);
                break;
            case PackageAction.DisallowLicense:
                DisallowLicense(package);
                break;
            case PackageAction.SkipPackage:
                SkipPackage(package);
                break;
            case PackageAction.ViewDetails:
                DisplayDetailedInfo(package);
                await ReviewPackageAsync(package); // Show menu again
                break;
            case PackageAction.SkipDecision:
                AnsiConsole.MarkupLine("[grey]Skipping...[/]\n");
                break;
            case PackageAction.Quit:
                AnsiConsole.MarkupLine("[yellow]Exiting interactive mode...[/]");
                break;
        }
    }

    private void DisplayPackageDetails(LicenseInfo package)
    {
        var panel = new Panel(
            new Markup(
                $"[bold]{package.PackageId}[/] [grey]v{package.PackageVersion}[/]\n"
                    + $"[yellow]License:[/] {package.License ?? "Unknown"}\n"
                    + $"[yellow]Reason:[/] {package.ProblemReason ?? "N/A"}\n"
                    + $"[yellow]Published:[/] {package.PublishDate?.ToString("yyyy-MM-dd") ?? "Unknown"}"
            )
        );
        panel.Header = new PanelHeader("Problematic Package");
        panel.Border = BoxBorder.Rounded;
        panel.BorderColor(Color.Red);

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    private void DisplayDetailedInfo(LicenseInfo package)
    {
        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.BorderColor(Color.Blue);
        table.AddColumn("Property");
        table.AddColumn("Value");

        table.AddRow("Package ID", package.PackageId);
        table.AddRow("Version", package.PackageVersion);
        table.AddRow("License", package.License ?? "Unknown");
        table.AddRow("License URL", package.LicenseUrl ?? "N/A");
        table.AddRow("Authors", package.Authors ?? "N/A");
        table.AddRow("Copyright", package.Copyright ?? "N/A");
        table.AddRow("Project URL", package.PackageProjectUrl ?? "N/A");
        table.AddRow("Repository", package.Repository ?? "N/A");
        table.AddRow("Publish Date", package.PublishDate?.ToString("yyyy-MM-dd") ?? "Unknown");
        table.AddRow("Age Status", package.AgeStatus.ToString());
        table.AddRow("Problem Reason", package.ProblemReason ?? "N/A");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Press any key to continue...");
        Console.ReadKey(true);
    }

    private async Task SafelistPackageAsync(LicenseInfo package)
    {
        var reason = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter reason for safelisting:")
                .Validate(reason =>
                {
                    return string.IsNullOrWhiteSpace(reason)
                        ? ValidationResult.Error("Reason cannot be empty")
                        : ValidationResult.Success();
                })
        );

        _config.Safelist[package.PackageId] = reason;
        _changes.Add(
            new ConfigChange
            {
                Type = ConfigChangeType.Safelist,
                PackageId = package.PackageId,
                Value = reason,
            }
        );

        AnsiConsole.MarkupLine(
            $"[green]✓ Package '{package.PackageId}' added to safelist[/]\n"
        );
    }

    private void AllowLicense(LicenseInfo package)
    {
        if (string.IsNullOrEmpty(package.License))
        {
            AnsiConsole.MarkupLine("[red]Cannot allow unknown license[/]\n");
            return;
        }

        if (_config.AllowedLicenses.Contains(package.License, StringComparer.OrdinalIgnoreCase))
        {
            AnsiConsole.MarkupLine(
                $"[yellow]License '{package.License}' is already in allowed list[/]\n"
            );
            return;
        }

        _config.AllowedLicenses.Add(package.License);
        _changes.Add(
            new ConfigChange
            {
                Type = ConfigChangeType.AllowLicense,
                Value = package.License,
            }
        );

        AnsiConsole.MarkupLine($"[green]✓ License '{package.License}' added to allowed list[/]\n");
    }

    private void DisallowLicense(LicenseInfo package)
    {
        if (string.IsNullOrEmpty(package.License))
        {
            AnsiConsole.MarkupLine("[red]Cannot disallow unknown license[/]\n");
            return;
        }

        if (
            _config.DisallowedLicenses.Contains(package.License, StringComparer.OrdinalIgnoreCase)
        )
        {
            AnsiConsole.MarkupLine(
                $"[yellow]License '{package.License}' is already in disallowed list[/]\n"
            );
            return;
        }

        _config.DisallowedLicenses.Add(package.License);
        _changes.Add(
            new ConfigChange
            {
                Type = ConfigChangeType.DisallowLicense,
                Value = package.License,
            }
        );

        AnsiConsole.MarkupLine(
            $"[green]✓ License '{package.License}' added to disallowed list[/]\n"
        );
    }

    private void SkipPackage(LicenseInfo package)
    {
        if (_config.Skiplist.Contains(package.PackageId))
        {
            AnsiConsole.MarkupLine(
                $"[yellow]Package '{package.PackageId}' is already in skiplist[/]\n"
            );
            return;
        }

        _config.Skiplist.Add(package.PackageId);
        _changes.Add(
            new ConfigChange
            {
                Type = ConfigChangeType.Skip,
                PackageId = package.PackageId,
            }
        );

        AnsiConsole.MarkupLine($"[green]✓ Package '{package.PackageId}' added to skiplist[/]\n");
    }

    private void ShowChangesSummary()
    {
        var panel = new Panel(BuildChangesSummary());
        panel.Header = new PanelHeader("Summary of Changes");
        panel.Border = BoxBorder.Double;
        panel.BorderColor(Color.Green);

        AnsiConsole.Write(panel);
    }

    private string BuildChangesSummary()
    {
        var summary = new List<string>();

        var safelistChanges = _changes.Where(c => c.Type == ConfigChangeType.Safelist).ToList();
        if (safelistChanges.Any())
        {
            summary.Add($"[bold]Safelisted packages:[/] {safelistChanges.Count}");
            foreach (var change in safelistChanges)
            {
                summary.Add($"  • [cyan]{change.PackageId}[/]: {change.Value}");
            }
            summary.Add("");
        }

        var allowedLicenses = _changes.Where(c => c.Type == ConfigChangeType.AllowLicense).ToList();
        if (allowedLicenses.Any())
        {
            summary.Add($"[bold]Allowed licenses:[/] {allowedLicenses.Count}");
            foreach (var change in allowedLicenses)
            {
                summary.Add($"  • [green]{change.Value}[/]");
            }
            summary.Add("");
        }

        var disallowedLicenses = _changes
            .Where(c => c.Type == ConfigChangeType.DisallowLicense)
            .ToList();
        if (disallowedLicenses.Any())
        {
            summary.Add($"[bold]Disallowed licenses:[/] {disallowedLicenses.Count}");
            foreach (var change in disallowedLicenses)
            {
                summary.Add($"  • [red]{change.Value}[/]");
            }
            summary.Add("");
        }

        var skipChanges = _changes.Where(c => c.Type == ConfigChangeType.Skip).ToList();
        if (skipChanges.Any())
        {
            summary.Add($"[bold]Skipped packages:[/] {skipChanges.Count}");
            foreach (var change in skipChanges)
            {
                summary.Add($"  • [grey]{change.PackageId}[/]");
            }
        }

        return string.Join("\n", summary);
    }

    private async Task SaveConfigurationAsync()
    {
        var configPath = Path.Combine(
            Environment.CurrentDirectory,
            ConfigurationService.ConfigFileName
        );

        var json = JsonSerializer.Serialize(
            _config,
            new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            }
        );

        await File.WriteAllTextAsync(configPath, json);
    }
}

public class InteractiveModeResult
{
    public bool ConfigUpdated { get; set; }
    public bool ShouldRerun { get; set; }
}

public enum PackageAction
{
    SafelistPackage,
    AllowLicense,
    DisallowLicense,
    SkipPackage,
    ViewDetails,
    SkipDecision,
    Quit,
}

public enum ConfigChangeType
{
    Safelist,
    AllowLicense,
    DisallowLicense,
    Skip,
}

public class ConfigChange
{
    public ConfigChangeType Type { get; set; }
    public string? PackageId { get; set; }
    public string? Value { get; set; }
}