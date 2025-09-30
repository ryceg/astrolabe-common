using Astrolabe.LicenseCheck.Models;

namespace Astrolabe.LicenseCheck.Services;

public class LicenseValidatorService
{
    private readonly LicenseCheckConfig _config;

    public LicenseValidatorService(LicenseCheckConfig config)
    {
        _config = config;
    }

    public void Validate(LicenseInfo licenseInfo)
    {
        // Check safelist first
        if (_config.Safelist.TryGetValue(licenseInfo.PackageId, out var reason))
        {
            licenseInfo.IsSafelisted = true;
            licenseInfo.IsProblematic = false; // Safelisted packages are not considered problematic for build failure purposes
            licenseInfo.ProblemReason = $"Safelisted: {reason}";
            return;
        }

        // Parse license string into individual licenses
        var license = licenseInfo.License ?? "UNKNOWN";
        var licenses = license
            .Split(new[] { " OR ", "/", " AND " }, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim().Replace("(", "").Replace(")", ""))
            .ToArray();

        // Check if any license is explicitly disallowed
        if (_config.DisallowedLicenses.Any())
        {
            var disallowedLicense = licenses.FirstOrDefault(l =>
                _config.DisallowedLicenses.Contains(l, StringComparer.OrdinalIgnoreCase)
            );

            if (disallowedLicense != null)
            {
                licenseInfo.IsProblematic = true;
                licenseInfo.ProblemReason =
                    $"License '{disallowedLicense}' is explicitly disallowed.";
                return;
            }
        }

        // Check license against allowed list
        var isAllowed = licenses.Any(l =>
            _config.AllowedLicenses.Contains(l, StringComparer.OrdinalIgnoreCase)
        );

        if (!isAllowed && licenses.Any())
        {
            licenseInfo.IsProblematic = true;
            licenseInfo.ProblemReason =
                $"License '{license}' is not in the list of allowed licenses.";
        }
    }
}
