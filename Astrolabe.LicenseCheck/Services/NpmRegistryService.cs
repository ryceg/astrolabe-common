using System.Text.Json;
using Astrolabe.LicenseCheck.Models;
using Spectre.Console;

namespace Astrolabe.LicenseCheck.Services;

public class NpmRegistryService
{
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, DateTime> _packageDateCache;
    private readonly SemaphoreSlim _rateLimiter;

    public NpmRegistryService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Astrolabe-LicenseCheck/1.0.0");
        _packageDateCache = new Dictionary<string, DateTime>();
        _rateLimiter = new SemaphoreSlim(5, 5); // Limit concurrent requests
    }

    public async Task<DateTime?> GetPackagePublishDateAsync(string packageName, string version)
    {
        var cacheKey = $"{packageName}@{version}";

        if (_packageDateCache.TryGetValue(cacheKey, out var cachedDate))
        {
            return cachedDate;
        }

        await _rateLimiter.WaitAsync();

        try
        {
            // Use npm registry API
            var url = $"https://registry.npmjs.org/{Uri.EscapeDataString(packageName)}";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var registryData = JsonSerializer.Deserialize<NpmRegistryResponse>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (
                registryData?.Time != null
                && registryData.Time.TryGetValue(version, out var dateString)
            )
            {
                if (DateTime.TryParse(dateString, out var publishDate))
                {
                    _packageDateCache[cacheKey] = publishDate;
                    return publishDate;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            // Don't fail the entire process for registry lookup failures
            if (Environment.GetEnvironmentVariable("VERBOSE") == "true")
            {
                AnsiConsole.MarkupLine(
                    $"[yellow]Failed to get publish date for {packageName}@{version}: {ex.Message}[/]"
                );
            }
            return null;
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    public async Task<List<PackageMetadata>> EnrichWithPublishDatesAsync(
        List<PackageMetadata> packages,
        bool verbose = false,
        ProgressContext? context = null
    )
    {
        if (!packages.Any())
            return packages;

        var totalPackages = packages.Count;
        var processedCount = 0;

        if (verbose)
        {
            AnsiConsole.MarkupLine(
                $"[yellow]Fetching publish dates for {totalPackages} packages...[/]"
            );
        }

        var action = new Func<ProgressTask, Task>(
            async (task) =>
            {
                task.MaxValue = totalPackages;

                // Process packages in batches to avoid overwhelming the registry
                var batches = packages.Chunk(10);

                foreach (var batch in batches)
                {
                    var tasks = batch.Select(async package =>
                    {
                        var publishDate = await GetPackagePublishDateAsync(
                            package.Name,
                            package.Version
                        );
                        if (publishDate.HasValue)
                        {
                            package.PublishDate = publishDate.Value;
                            AnalyzePackageAge(package);
                        }

                        Interlocked.Increment(ref processedCount);
                        task.Value = processedCount;
                    });

                    await Task.WhenAll(tasks);

                    // Small delay between batches to be respectful to the registry
                    await Task.Delay(100);
                }
            }
        );

        if (context != null)
        {
            var task = context.AddTask("[green]Fetching npm package dates[/]");
            await action(task);
        }
        else
        {
            await AnsiConsole.Progress().Columns(
                new ProgressColumn[]
                {
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new SpinnerColumn(),
                }
            ).StartAsync(async ctx =>
            {
                var task = ctx.AddTask("[green]Fetching package dates[/]");
                await action(task);
            });
        }

        if (verbose)
        {
            var enrichedCount = packages.Count(p => p.PublishDate.HasValue);
            AnsiConsole.MarkupLine(
                $"[green]Successfully enriched {enrichedCount}/{totalPackages} packages with publish dates[/]"
            );

            // Show security analysis summary
            ShowSecurityAnalysisSummary(packages);
        }

        return packages;
    }

    private void AnalyzePackageAge(PackageMetadata package)
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

    private void ShowSecurityAnalysisSummary(List<PackageMetadata> packages)
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

    public void Dispose()
    {
        _httpClient?.Dispose();
        _rateLimiter?.Dispose();
    }
}
