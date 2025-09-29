namespace Astrolabe.LicenseCheck.Models;

public class ReportOptions
{
    public string OutputDirectory { get; set; } = "./license-reports";
    public bool IncludeTransitive { get; set; }
    public OutputFormat Format { get; set; } = OutputFormat.All;
    public bool Verbose { get; set; }
    public bool ExcludePrivatePackages { get; set; }
    public bool ProductionOnly { get; set; }
    public bool DevelopmentOnly { get; set; }
}

public enum OutputFormat
{
    Json,
    Csv,
    Excel,
    All,
}
