using System.Text.Json;
using Astrolabe.LicenseCheck.Models;
using Spectre.Console;

namespace Astrolabe.LicenseCheck.Services;

public class NugetRegistryService
{
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, DateTime> _packageDateCache;
    private readonly SemaphoreSlim _rateLimiter;
    private readonly string _cacheFilePath;

    public NugetRegistryService(string? cacheDirectory = null)
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Astrolabe-LicenseCheck/1.0.0");
        _packageDateCache = new Dictionary<string, DateTime>();
        _rateLimiter = new SemaphoreSlim(10, 10); // Limit concurrent requests

        // If cacheDirectory is explicitly null, disable caching entirely
        if (cacheDirectory == null)
        {
            _cacheFilePath = string.Empty;
        }
        else
        {
            // Use specified cache directory or temp directory as fallback
            var cacheDir = string.IsNullOrEmpty(cacheDirectory)
                ? Path.Combine(Path.GetTempPath(), "astrolabe-license-check")
                : Path.GetFullPath(cacheDirectory);

            Directory.CreateDirectory(cacheDir);
            _cacheFilePath = Path.Combine(cacheDir, "nuget-package-dates-cache.json");

            LoadCacheFromDisk();
        }
    }

    private void LoadCacheFromDisk()
    {
        try
        {
            if (File.Exists(_cacheFilePath))
            {
                var json = File.ReadAllText(_cacheFilePath);
                var cache = JsonSerializer.Deserialize<Dictionary<string, DateTime>>(json);
                if (cache != null)
                {
                    foreach (var kvp in cache)
                    {
                        _packageDateCache[kvp.Key] = kvp.Value;
                    }
                }
            }
        }
        catch
        {
            // Ignore cache load errors, will just refetch
        }
    }

    private void SaveCacheToDisk()
    {
        if (string.IsNullOrEmpty(_cacheFilePath))
            return; // Caching disabled

        try
        {
            var json = JsonSerializer.Serialize(_packageDateCache, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_cacheFilePath, json);
        }
        catch
        {
            // Ignore cache save errors
        }
    }

    public async Task<DateTime?> GetPackagePublishDateAsync(string packageId, string version)
    {
        var cacheKey = $"{packageId}@{version}";
        if (_packageDateCache.TryGetValue(cacheKey, out var cachedDate))
        {
            return cachedDate;
        }

        await _rateLimiter.WaitAsync();
        try
        {
            var url =
                $"https://api.nuget.org/v3/registration5-semver1/{packageId.ToLowerInvariant()}/{version.ToLowerInvariant()}.json";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("published", out var publishedElement))
            {
                if (publishedElement.TryGetDateTime(out var publishDate))
                {
                    _packageDateCache[cacheKey] = publishDate;
                    return publishDate;
                }
            }
            return null;
        }
        catch (Exception ex)
        {
            if (Environment.GetEnvironmentVariable("VERBOSE") == "true")
            {
                AnsiConsole.MarkupLine(
                    $"[yellow]Failed to get publish date for {packageId}@{version}: {ex.Message}[/]"
                );
            }
            return null;
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    public async Task<List<LicenseInfo>> EnrichWithPublishDatesAsync(
        List<LicenseInfo> licenses,
        bool verbose = false,
        ProgressContext? context = null
    )
    {
        if (!licenses.Any())
            return licenses;

        // Filter to only packages that need date fetching
        var packagesNeedingDates = licenses.Where(l => !l.PublishDate.HasValue).ToList();

        if (!packagesNeedingDates.Any())
        {
            if (verbose)
            {
                AnsiConsole.MarkupLine(
                    $"[grey]All {licenses.Count} packages already have publish dates (using cached data)[/]"
                );
            }
            return licenses;
        }

        if (verbose)
        {
            var cachedCount = licenses.Count - packagesNeedingDates.Count;
            if (cachedCount > 0)
            {
                AnsiConsole.MarkupLine(
                    $"[grey]Using cached dates for {cachedCount} package(s), fetching {packagesNeedingDates.Count} new package(s)...[/]"
                );
            }
            else
            {
                AnsiConsole.MarkupLine(
                    $"[yellow]Fetching publish dates for {packagesNeedingDates.Count} NuGet packages...[/]"
                );
            }
        }

        var action = new Func<ProgressTask, Task>(
            async (task) =>
            {
                task.MaxValue = packagesNeedingDates.Count;
                var tasks = packagesNeedingDates.Select(async license =>
                {
                    var publishDate = await GetPackagePublishDateAsync(
                        license.PackageId,
                        license.PackageVersion
                    );
                    if (publishDate.HasValue)
                    {
                        license.PublishDate = publishDate.Value;
                        AnalyzePackageAge(license);
                    }
                    task.Increment(1);
                });
                await Task.WhenAll(tasks);
            }
        );

        if (context != null)
        {
            var task = context.AddTask("[green]Fetching NuGet package dates[/]");
            await action(task);
        }
        else
        {
            await AnsiConsole.Progress().StartAsync(async ctx =>
            {
                var task = ctx.AddTask("[green]Fetching NuGet package dates[/]");
                await action(task);
            });
        }

        // Save cache to disk after fetching new dates
        SaveCacheToDisk();

        // Analyze age for packages that already had dates cached
        foreach (var license in licenses.Where(l => l.PublishDate.HasValue && l.AgeStatus == PackageAgeStatus.Unknown))
        {
            AnalyzePackageAge(license);
        }

        if (verbose)
        {
            var enrichedCount = licenses.Count(p => p.PublishDate.HasValue);
            AnsiConsole.MarkupLine(
                $"[green]Successfully enriched {enrichedCount}/{licenses.Count} NuGet packages with publish dates[/]"
            );
            ShowSecurityAnalysisSummary(licenses);
        }

        return licenses;
    }

    private void AnalyzePackageAge(LicenseInfo package)
    {
        if (!package.PublishDate.HasValue)
        {
            package.AgeStatus = PackageAgeStatus.Unknown;
            return;
        }

        var age = DateTime.UtcNow - package.PublishDate.Value;

        package.AgeStatus = age.TotalDays switch
        {
            < 7 => PackageAgeStatus.Fresh,
            >= 7 and <= 730 => PackageAgeStatus.Normal, // 2 years
            > 730 and <= 1095 => PackageAgeStatus.Stale, // 3 years
            > 1095 => PackageAgeStatus.Outdated,
            _ => PackageAgeStatus.Unknown,
        };
    }

    private void ShowSecurityAnalysisSummary(List<LicenseInfo> packages)
    {
        var freshCount = packages.Count(p => p.AgeStatus == PackageAgeStatus.Fresh);
        var staleCount = packages.Count(p => p.AgeStatus == PackageAgeStatus.Stale);
        var outdatedCount = packages.Count(p => p.AgeStatus == PackageAgeStatus.Outdated);

        if (freshCount > 0)
        {
            AnsiConsole.MarkupLine(
                $"[yellow]⚠️  {freshCount} very recent packages found (< 7 days old)[/]"
            );
        }
        if (staleCount > 0)
        {
            AnsiConsole.MarkupLine(
                $"[orange3]📅 {staleCount} stale packages found (2-3 years old)[/]"
            );
        }
        if (outdatedCount > 0)
        {
            AnsiConsole.MarkupLine(
                $"[red]🚨 {outdatedCount} very old packages found (> 3 years old)[/]"
            );
        }
        if (freshCount == 0 && staleCount == 0 && outdatedCount == 0)
        {
            AnsiConsole.MarkupLine("[green]✅ All packages appear to have normal age[/]");
        }
    }
}
