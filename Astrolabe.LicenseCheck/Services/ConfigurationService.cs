using System.Text.Json;
using Astrolabe.LicenseCheck.Models;
using Spectre.Console;

namespace Astrolabe.LicenseCheck.Services;

public class ConfigurationService
{
    public const string ConfigFileName = "license-check.json";

    public LicenseCheckConfig LoadConfig(string basePath, dynamic? commandLineSettings = null)
    {
        var defaultConfig = GetDefaultConfig();
        var configFilePath = Path.Combine(basePath, ConfigFileName);

        if (File.Exists(configFilePath))
        {
            try
            {
                var json = File.ReadAllText(configFilePath);
                var userConfig = JsonSerializer.Deserialize<LicenseCheckConfig>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        AllowTrailingCommas = true,
                        ReadCommentHandling = JsonCommentHandling.Skip,
                    }
                );

                if (userConfig != null)
                {
                    // User lists replace defaults if they are provided.
                    if (userConfig.AllowedLicenses.Any())
                        defaultConfig.AllowedLicenses = userConfig.AllowedLicenses;
                    if (userConfig.DisallowedLicenses.Any())
                        defaultConfig.DisallowedLicenses = userConfig.DisallowedLicenses;
                    if (userConfig.Skiplist.Any())
                        defaultConfig.Skiplist = userConfig.Skiplist;
                    if (userConfig.Safelist.Any())
                        defaultConfig.Safelist = userConfig.Safelist;
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine(
                    $"[yellow]Warning: Could not load or parse '{ConfigFileName}'. Using default configuration. Error: {ex.Message}[/]"
                );
            }
        }

        // Command-line arguments override config file
        if (commandLineSettings != null)
        {
            if (commandLineSettings.AllowedLicenses?.Length > 0)
            {
                defaultConfig.AllowedLicenses = new List<string>(commandLineSettings.AllowedLicenses);
            }

            if (commandLineSettings.DisallowedLicenses?.Length > 0)
            {
                defaultConfig.DisallowedLicenses = new List<string>(commandLineSettings.DisallowedLicenses);
            }

            if (commandLineSettings.Skiplist?.Length > 0)
            {
                defaultConfig.Skiplist = new List<string>(commandLineSettings.Skiplist);
            }

            if (commandLineSettings.Safelist?.Length > 0)
            {
                defaultConfig.Safelist = ParseSafelist(commandLineSettings.Safelist);
            }
        }

        return defaultConfig;
    }

    private Dictionary<string, string> ParseSafelist(string[] safelistArgs)
    {
        var safelist = new Dictionary<string, string>();

        foreach (var arg in safelistArgs)
        {
            var parts = arg.Split('=', 2);
            if (parts.Length == 2)
            {
                safelist[parts[0].Trim()] = parts[1].Trim();
            }
            else
            {
                // If no reason provided, use a default reason
                safelist[parts[0].Trim()] = "Safelisted via command line";
            }
        }

        return safelist;
    }

    private LicenseCheckConfig GetDefaultConfig()
    {
        return new LicenseCheckConfig
        {
            AllowedLicenses = new List<string>
            {
                "MIT",
                "Apache-2.0",
                "BSD-3-Clause",
                "BSD-2-Clause",
                "ISC",
                "MS-PL",
            },
        };
    }
}
