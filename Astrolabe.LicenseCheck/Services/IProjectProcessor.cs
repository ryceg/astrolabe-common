using Astrolabe.LicenseCheck.Models;
using Spectre.Console;

namespace Astrolabe.LicenseCheck.Services;

public interface IProjectProcessor
{
    bool CanProcess(string filePath);
    Task<LicenseReport> ProcessAsync(
        string filePath,
        ReportOptions options,
        LicenseCheckConfig config,
        ProgressContext? context = null
    );
}
