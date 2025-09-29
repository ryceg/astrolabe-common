using System.Text.Json;
using Astrolabe.LicenseCheck.Models;
using Spectre.Console;

namespace Astrolabe.LicenseCheck.Services;

public class ConfigurationService
{
    public const string ConfigFileName = "license-check.json";

    public LicenseCheckConfig LoadConfig(string basePath)
    {
        var defaultConfig = GetDefaultConfig();
        var configFilePath = Path.Combine(basePath, ConfigFileName);

        if (!File.Exists(configFilePath))
        {
            return defaultConfig;
        }

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

        return defaultConfig;
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
