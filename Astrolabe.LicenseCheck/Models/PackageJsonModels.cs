using System.Text.Json.Serialization;

namespace Astrolabe.LicenseCheck.Models;

public class PackageJson
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("license")]
    public object? License { get; set; } // Can be string or LicenseObject

    [JsonPropertyName("licenses")]
    public object[]? Licenses { get; set; } // Can be array of strings or LicenseObjects

    [JsonPropertyName("author")]
    public object? Author { get; set; } // Can be string or AuthorObject

    [JsonPropertyName("contributors")]
    public object[]? Contributors { get; set; }

    [JsonPropertyName("repository")]
    public object? Repository { get; set; } // Can be string or RepositoryObject

    [JsonPropertyName("homepage")]
    public string? Homepage { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("readme")]
    public string? Readme { get; set; }

    [JsonPropertyName("readmeFilename")]
    public string? ReadmeFilename { get; set; }

    [JsonPropertyName("dependencies")]
    public Dictionary<string, string>? Dependencies { get; set; }

    [JsonPropertyName("devDependencies")]
    public Dictionary<string, string>? DevDependencies { get; set; }

    [JsonPropertyName("peerDependencies")]
    public Dictionary<string, string>? PeerDependencies { get; set; }

    [JsonPropertyName("optionalDependencies")]
    public Dictionary<string, string>? OptionalDependencies { get; set; }

    [JsonPropertyName("private")]
    public bool? Private { get; set; }
}

public class LicenseObject
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

public class AuthorObject
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

public class RepositoryObject
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("directory")]
    public string? Directory { get; set; }
}

public class PackageMetadata
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string? License { get; set; }
    public string? Author { get; set; }
    public string? Repository { get; set; }
    public string? Homepage { get; set; }
    public string? Description { get; set; }
    public string PackagePath { get; set; } = string.Empty;
    public bool LicenseFromFile { get; set; }

    /// <summary>
    /// Indicates if license was inferred from file
    /// </summary>
    public string? LicenseFile { get; set; }

    /// <summary>
    /// Version date information for security analysis
    /// </summary>
    public DateTime? PublishDate { get; set; }
    public PackageAgeStatus AgeStatus { get; set; }
}

public enum PackageAgeStatus
{
    /// <summary>
    /// Unknown publish date
    /// </summary>
    Unknown,

    /// <summary>
    /// Published very recently (< 7 days)
    /// </summary>
    Fresh,

    /// <summary>
    /// Normal age (7 days - 2 years)
    /// </summary>
    Normal,

    /// <summary>
    /// Old but acceptable (2-3 years)
    /// </summary>
    Stale,

    /// <summary>
    /// Very old (> 3 years)
    /// </summary>
    Outdated,
}

// NPM Registry API response models
public class NpmRegistryResponse
{
    [JsonPropertyName("time")]
    public Dictionary<string, string>? Time { get; set; }

    [JsonPropertyName("versions")]
    public Dictionary<string, object>? Versions { get; set; }
}
