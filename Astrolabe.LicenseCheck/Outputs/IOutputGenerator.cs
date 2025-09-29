using Astrolabe.LicenseCheck.Models;

namespace Astrolabe.LicenseCheck.Outputs;

public interface IOutputGenerator
{
    bool SupportsFormat(OutputFormat format);
    Task GenerateAsync(List<LicenseReport> reports, ReportOptions options);
}
