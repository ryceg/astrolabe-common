using System.Text.Json.Serialization;

namespace Astrolabe.LicenseCheck.Models;

public class LicenseReport
{
    public string ProjectPath { get; set; } = string.Empty;
    public ProjectType ProjectType { get; set; }
    public List<LicenseInfo> Licenses { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class LicenseInfo
{
    public string PackageId { get; set; } = string.Empty;
    public string PackageVersion { get; set; } = string.Empty;
    public string? License { get; set; }
    public string? LicenseUrl { get; set; }
    public string? Authors { get; set; }
    public string? Copyright { get; set; }
    public string? PackageProjectUrl { get; set; }
    public string? Repository { get; set; }
    public LicenseInformationOrigin LicenseInformationOrigin { get; set; }

    // Package version date information for security analysis
    public DateTime? PublishDate { get; set; }
    public PackageAgeStatus AgeStatus { get; set; }

    // New properties for validation
    public bool IsSafelisted { get; set; }
    public bool IsProblematic { get; set; }
    public string? ProblemReason { get; set; }
}

// Models for nuget-license JSON output
public class NugetLicenseInfo
{
    [JsonPropertyName("PackageId")]
    public string PackageId { get; set; } = string.Empty;

    [JsonPropertyName("PackageVersion")]
    public string PackageVersion { get; set; } = string.Empty;

    [JsonPropertyName("PackageProjectUrl")]
    public string? PackageProjectUrl { get; set; }

    [JsonPropertyName("Copyright")]
    public string? Copyright { get; set; }

    [JsonPropertyName("Authors")]
    public string? Authors { get; set; }

    [JsonPropertyName("License")]
    public string? License { get; set; }

    [JsonPropertyName("LicenseUrl")]
    public string? LicenseUrl { get; set; }

    [JsonPropertyName("LicenseInformationOrigin")]
    public LicenseInformationOrigin LicenseInformationOrigin { get; set; }
}

// Models for license-checker JSON output
public class NpmLicenseInfo
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string? Licenses { get; set; }
    public string? Repository { get; set; }
    public string? Publisher { get; set; }
    public string? Email { get; set; }
    public string? Url { get; set; }
    public string? Path { get; set; }
    public string? LicenseFile { get; set; }
}

public enum ProjectType
{
    DotNet,
    Rush,
    Npm,
}

public enum LicenseInformationOrigin
{
    Unknown = 0,
    License = 1,
    PackageLicense = 2,
    PackageLicenseExpression = 3,
    PackageLicenseFile = 4,
    PackageLicenseUrl = 5,
}
