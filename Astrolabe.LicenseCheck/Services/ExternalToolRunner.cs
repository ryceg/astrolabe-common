using System.Diagnostics;
using System.Runtime.InteropServices;
using Spectre.Console;

namespace Astrolabe.LicenseCheck.Services;

public class ExternalToolRunner
{
    public async Task<bool> IsNugetLicenseAvailableAsync()
    {
        try
        {
            var result = await RunProcessAsync("dotnet", "tool list --global");
            return result.Output.Contains("nuget-license");
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> IsLicenseCheckerAvailableAsync()
    {
        try
        {
            var npmCommand = GetNpmCommand();
            var result = await RunProcessAsync(npmCommand, "list -g license-checker");
            return result.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> IsNodeJsAvailableAsync()
    {
        try
        {
            var result = await RunProcessAsync("node", "--version");
            return result.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<ProcessResult> RunNugetLicenseAsync(
        string inputPath,
        bool includeTransitive,
        string outputPath
    )
    {
        var arguments = $"--input \"{inputPath}\" --output JsonPretty";
        if (includeTransitive)
        {            arguments += " --include-transitive";
        }

        var result = await RunProcessAsync("nuget-license", arguments);

        // nuget-license may write JSON to stdout on success or stderr on failure.
        // A non-zero exit code can mean warnings (e.g., license not found), but still produce a valid report.
        string? jsonOutput = null;
        if (!string.IsNullOrWhiteSpace(result.Output) && result.Output.Trim().StartsWith("["))
        {
            jsonOutput = result.Output;
        }
        else if (!string.IsNullOrWhiteSpace(result.Error) && result.Error.Trim().StartsWith("["))
        {
            jsonOutput = result.Error;
        }


        if (!string.IsNullOrEmpty(jsonOutput))
        {
            await File.WriteAllTextAsync(outputPath, jsonOutput);
            // If we got a JSON report, we can ignore the exit code, as it may just indicate warnings.
            result.ExitCode = 0;
        }

        return result;
    }

    public async Task<ProcessResult> RunLicenseCheckerAsync(string projectPath, string outputPath)
    {
        var npmCommand = GetNpmCommand();
        var licenseCheckerCommand = GetLicenseCheckerCommand();

        var result = await RunProcessAsync(licenseCheckerCommand, "--json", projectPath);

        if (result.ExitCode == 0 && !string.IsNullOrEmpty(result.Output))
        {
            await File.WriteAllTextAsync(outputPath, result.Output);
        }

        return result;
    }

    public async Task<string?> FindExecutableAsync(string toolName)
    {
        try
        {
            var whereCommand = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "where"
                : "which";
            var result = await RunProcessAsync(whereCommand, toolName);

            if (result.ExitCode == 0 && !string.IsNullOrEmpty(result.Output))
            {
                return result.Output.Split('\n')[0].Trim();
            }
        }
        catch
        {
            // Tool not found
        }

        return null;
    }

    private async Task<ProcessResult> RunProcessAsync(
        string fileName,
        string arguments,
        string? workingDirectory = null
    )
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
        };

        using var process = new Process { StartInfo = startInfo };

        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
                outputBuilder.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
                errorBuilder.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        return new ProcessResult
        {
            ExitCode = process.ExitCode,
            Output = outputBuilder.ToString(),
            Error = errorBuilder.ToString(),
        };
    }

    private string GetNpmCommand()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "npm.cmd" : "npm";
    }

    private string GetLicenseCheckerCommand()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "license-checker.cmd"
            : "license-checker";
    }
}

public class ProcessResult
{
    public int ExitCode { get; set; }
    public string Output { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}
