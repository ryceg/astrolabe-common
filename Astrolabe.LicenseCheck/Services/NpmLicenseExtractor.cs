using System.Text.Json;
using System.Text.RegularExpressions;
using Astrolabe.LicenseCheck.Models;
using Spectre.Console;

namespace Astrolabe.LicenseCheck.Services;

public class NpmLicenseExtractor
{
    private static readonly string[] LicenseFileNames =
    {
        "LICENSE",
        "LICENCE",
        "LICENSE.md",
        "LICENCE.md",
        "LICENSE.txt",
        "LICENCE.txt",
        "COPYING",
        "COPYING.md",
        "COPYING.txt",
        "COPYRIGHT",
        "COPYRIGHT.md",
        "COPYRIGHT.txt",
    };

    private static readonly string[] ReadmeFileNames =
    {
        "README.md",
        "README.txt",
        "README",
        "readme.md",
        "readme.txt",
        "readme",
    };

    private static readonly Regex LicensePatterns = new(
        @"(?i)(?:license|licence)(?:\s*:?\s*)([A-Z0-9\-\.]+(?:\s+[A-Z0-9\-\.]+)*)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    public async Task<List<PackageMetadata>> ExtractFromDirectoryAsync(
        string projectPath,
        LicenseCheckConfig config,
        ReportOptions options
    )
    {
        var packages = new List<PackageMetadata>();
        var nodeModulesPath = Path.Combine(projectPath, "node_modules");

        if (!Directory.Exists(nodeModulesPath))
        {
            return packages;
        }

        var seenPackages = new HashSet<string>(); // Avoid duplicate packages

        var mainPackageJsonPath = Path.Combine(projectPath, "package.json");
        var targetDependencies = await GetTargetDependenciesAsync(
            mainPackageJsonPath,
            options
        );

        await ScanNodeModulesAsync(nodeModulesPath, packages, seenPackages, targetDependencies, config, options);

        return packages;
    }

    private async Task<HashSet<string>> GetTargetDependenciesAsync(
        string packageJsonPath,
        ReportOptions options
    )
    {
        var dependencies = new HashSet<string>();

        if (!File.Exists(packageJsonPath))
        {
            return dependencies;
        }

        try
        {
            var packageJson = await ParsePackageJsonAsync(packageJsonPath);

            if (options.DevelopmentOnly)
            {
                if (packageJson.DevDependencies != null)
                {
                    foreach (var dep in packageJson.DevDependencies.Keys)
                        dependencies.Add(dep);
                }
                return dependencies;
            }

            if (options.ProductionOnly)
            {
                if (packageJson.Dependencies != null)
                {
                    foreach (var dep in packageJson.Dependencies.Keys)
                        dependencies.Add(dep);
                }
                return dependencies;
            }

            // Default: include prod, peer, and optional. Dev is excluded unless specified.
            if (packageJson.Dependencies != null)
            {
                foreach (var dep in packageJson.Dependencies.Keys)
                    dependencies.Add(dep);
            }
            if (packageJson.PeerDependencies != null)
            {
                foreach (var dep in packageJson.PeerDependencies.Keys)
                    dependencies.Add(dep);
            }
            if (packageJson.OptionalDependencies != null)
            {
                foreach (var dep in packageJson.OptionalDependencies.Keys)
                    dependencies.Add(dep);
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine(
                $"[yellow]Warning: Could not parse main package.json: {ex.Message}[/]"
            );
        }

        return dependencies;
    }

    private async Task ScanNodeModulesAsync(
        string nodeModulesPath,
        List<PackageMetadata> packages,
        HashSet<string> seenPackages,
        HashSet<string> targetDependencies,
        LicenseCheckConfig config,
        ReportOptions options
    )
    {
        var directories = Directory.GetDirectories(nodeModulesPath);

        foreach (var dir in directories)
        {
            var dirName = Path.GetFileName(dir);

            if (dirName.StartsWith("@"))
            {
                var scopedDirs = Directory.GetDirectories(dir);
                foreach (var scopedDir in scopedDirs)
                {
                    var scopedName = $"{dirName}/{Path.GetFileName(scopedDir)}";
                    await ProcessPackageDirectoryAsync(
                        scopedDir,
                        scopedName,
                        packages,
                        seenPackages,
                        targetDependencies,
                        config,
                        options
                    );
                }
            }
            else
            {
                await ProcessPackageDirectoryAsync(
                    dir,
                    dirName,
                    packages,
                    seenPackages,
                    targetDependencies,
                    config,
                    options
                );
            }
        }
    }

    private async Task ProcessPackageDirectoryAsync(
        string packageDir,
        string packageName,
        List<PackageMetadata> packages,
        HashSet<string> seenPackages,
        HashSet<string> targetDependencies,
        LicenseCheckConfig config,
        ReportOptions options
    )
    {
        var packageJsonPath = Path.Combine(packageDir, "package.json");

        if (!File.Exists(packageJsonPath))
        {
            return;
        }

        try
        {
            var packageJson = await ParsePackageJsonAsync(packageJsonPath);
            var fullPackageName = $"{packageJson.Name}@{packageJson.Version}";

            if (options.ExcludePrivatePackages && packageJson.Private == true)
            {
                return;
            }
            
            if (config.Skiplist.Contains(packageJson.Name ?? ""))
            {
                return;
            }

            if (seenPackages.Contains(fullPackageName))
            {
                return;
            }

            if (targetDependencies.Any() && !targetDependencies.Contains(packageJson.Name ?? ""))
            {
                return;
            }

            seenPackages.Add(fullPackageName);

            var metadata = await ExtractPackageMetadataAsync(packageDir, packageJson);
            packages.Add(metadata);

            var nestedNodeModules = Path.Combine(packageDir, "node_modules");
            if (Directory.Exists(nestedNodeModules))
            {
                await ScanNodeModulesAsync(
                    nestedNodeModules,
                    packages,
                    seenPackages,
                    new HashSet<string>(), // For nested dependencies, we scan everything
                    config,
                    options
                );
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine(
                $"[yellow]Warning: Could not process package {packageName}: {ex.Message}[/]"
            );
        }
    }

    private async Task<PackageMetadata> ExtractPackageMetadataAsync(
        string packageDir,
        PackageJson packageJson
    )
    {
        var metadata = new PackageMetadata
        {
            Name = packageJson.Name ?? "unknown",
            Version = packageJson.Version ?? "unknown",
            PackagePath = packageDir,
            Description = packageJson.Description,
        };

        // Extract license information using fallback strategy
        metadata.License = await ExtractLicenseAsync(packageDir, packageJson, metadata);

        // Extract author information
        metadata.Author = ExtractAuthor(packageJson);

        // Extract repository information
        metadata.Repository = ExtractRepository(packageJson);

        // Set homepage
        metadata.Homepage = packageJson.Homepage;

        return metadata;
    }

    private async Task<string?> ExtractLicenseAsync(
        string packageDir,
        PackageJson packageJson,
        PackageMetadata metadata
    )
    {
        // Strategy 1: Check package.json license field
        var license = ExtractLicenseFromPackageJson(packageJson);
        if (!string.IsNullOrEmpty(license))
        {
            return license;
        }

        // Strategy 2: Look for license files
        var licenseFromFile = await ExtractLicenseFromFilesAsync(packageDir, metadata);
        if (!string.IsNullOrEmpty(licenseFromFile))
        {
            metadata.LicenseFromFile = true;
            return licenseFromFile + "*"; // Add asterisk to indicate inference
        }

        // Strategy 3: Check README content
        var licenseFromReadme = await ExtractLicenseFromReadmeAsync(packageDir, packageJson);
        if (!string.IsNullOrEmpty(licenseFromReadme))
        {
            metadata.LicenseFromFile = true;
            return licenseFromReadme + "*"; // Add asterisk to indicate inference
        }

        return "UNKNOWN";
    }

    private string? ExtractLicenseFromPackageJson(PackageJson packageJson)
    {
        // Handle single license field
        if (packageJson.License != null)
        {
            return ExtractLicenseValue(packageJson.License);
        }

        // Handle licenses array (deprecated but still used)
        if (packageJson.Licenses != null && packageJson.Licenses.Length > 0)
        {
            var licenses = packageJson
                .Licenses.Select(ExtractLicenseValue)
                .Where(l => !string.IsNullOrEmpty(l))
                .ToList();

            return licenses.Any() ? string.Join(" OR ", licenses) : null;
        }

        return null;
    }

    private string? ExtractLicenseValue(object licenseValue)
    {
        if (licenseValue is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.String)
            {
                return element.GetString();
            }
            else if (element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty("type", out var typeProperty))
                {
                    return typeProperty.GetString();
                }
                if (element.TryGetProperty("name", out var nameProperty))
                {
                    return nameProperty.GetString();
                }
            }
        }
        else if (licenseValue is string stringValue)
        {
            return stringValue;
        }

        return null;
    }

    private async Task<string?> ExtractLicenseFromFilesAsync(
        string packageDir,
        PackageMetadata metadata
    )
    {
        foreach (var fileName in LicenseFileNames)
        {
            var filePath = Path.Combine(packageDir, fileName);
            if (File.Exists(filePath))
            {
                metadata.LicenseFile = filePath;
                try
                {
                    var content = await File.ReadAllTextAsync(filePath);
                    var license = ExtractLicenseFromText(content);
                    if (!string.IsNullOrEmpty(license))
                    {
                        return license;
                    }
                }
                catch
                {
                    // Ignore file reading errors
                }
            }
        }

        return null;
    }

    private async Task<string?> ExtractLicenseFromReadmeAsync(
        string packageDir,
        PackageJson packageJson
    )
    {
        // First try the readme content from package.json
        if (!string.IsNullOrEmpty(packageJson.Readme))
        {
            var license = ExtractLicenseFromText(packageJson.Readme);
            if (!string.IsNullOrEmpty(license))
            {
                return license;
            }
        }

        // Then try readme files
        foreach (var fileName in ReadmeFileNames)
        {
            var filePath = Path.Combine(packageDir, fileName);
            if (File.Exists(filePath))
            {
                try
                {
                    var content = await File.ReadAllTextAsync(filePath);
                    var license = ExtractLicenseFromText(content);
                    if (!string.IsNullOrEmpty(license))
                    {
                        return license;
                    }
                }
                catch
                {
                    // Ignore file reading errors
                }
            }
        }

        return null;
    }

    private string? ExtractLicenseFromText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return null;

        var matches = LicensePatterns.Matches(text);
        if (matches.Count > 0)
        {
            var license = matches[0].Groups[1].Value.Trim();

            // Clean up common patterns
            license = license.Replace("License", "").Replace("license", "").Trim();

            // Return first valid-looking license identifier
            if (license.Length > 1 && license.Length < 50)
            {
                return license;
            }
        }

        // Look for common license indicators
        var upperText = text.ToUpperInvariant();
        if (upperText.Contains("MIT LICENSE"))
            return "MIT";
        if (upperText.Contains("APACHE LICENSE"))
            return "Apache-2.0";
        if (upperText.Contains("BSD LICENSE"))
            return "BSD";
        if (upperText.Contains("GPL"))
            return "GPL";
        if (upperText.Contains("ISC LICENSE"))
            return "ISC";

        return null;
    }

    private string? ExtractAuthor(PackageJson packageJson)
    {
        if (packageJson.Author != null)
        {
            if (packageJson.Author is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.String)
                {
                    return element.GetString();
                }
                else if (element.ValueKind == JsonValueKind.Object)
                {
                    if (element.TryGetProperty("name", out var nameProperty))
                    {
                        return nameProperty.GetString();
                    }
                }
            }
            else if (packageJson.Author is string stringValue)
            {
                return stringValue;
            }
        }

        return null;
    }

    private string? ExtractRepository(PackageJson packageJson)
    {
        if (packageJson.Repository != null)
        {
            if (packageJson.Repository is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.String)
                {
                    return element.GetString();
                }
                else if (element.ValueKind == JsonValueKind.Object)
                {
                    if (element.TryGetProperty("url", out var urlProperty))
                    {
                        return urlProperty.GetString();
                    }
                }
            }
            else if (packageJson.Repository is string stringValue)
            {
                return stringValue;
            }
        }

        return null;
    }

    private async Task<PackageJson> ParsePackageJsonAsync(string packageJsonPath)
    {
        var json = await File.ReadAllTextAsync(packageJsonPath);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        return JsonSerializer.Deserialize<PackageJson>(json, options) ?? new PackageJson();
    }
}
