using Astrolabe.LicenseCheck.Models;

namespace Astrolabe.LicenseCheck.Outputs;

public interface IOutputGenerator
{
    Task GenerateAsync(List<LicenseReport> reports, ReportOptions options);
}
