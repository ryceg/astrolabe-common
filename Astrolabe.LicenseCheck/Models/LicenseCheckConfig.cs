using System.Text.Json.Serialization;

namespace Astrolabe.LicenseCheck.Models;

public class LicenseCheckConfig
{
    [JsonPropertyName("allowedLicenses")]
    public List<string> AllowedLicenses { get; set; } = new();

    [JsonPropertyName("disallowedLicenses")]
    public List<string> DisallowedLicenses { get; set; } = new();

    [JsonPropertyName("skiplist")]
    public List<string> Skiplist { get; set; } = new();

    [JsonPropertyName("safelist")]
    public Dictionary<string, string> Safelist { get; set; } = new();
}
