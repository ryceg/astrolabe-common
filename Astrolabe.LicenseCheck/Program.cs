using Astrolabe.LicenseCheck.Commands;
using Spectre.Console.Cli;

var app = new CommandApp<LicenseCheckCommand>();
app.Configure(config =>
{
    config.SetApplicationName("astrolabe-license-check");
    config.SetApplicationVersion("1.0.0");

    config.AddExample(new[] { "--output-dir", "./reports" });
    config.AddExample(new[] { "--include-transitive", "MyProject.sln" });
    config.AddExample(new[] { "MyProject.csproj", "ClientApp/rush.json" });

    // Add new examples
    config.AddExample(Array.Empty<string>()); // Represents running with no arguments
    config.AddExample(new[] { "--format", "Csv" });
    config.AddExample(new[] { "--include-transitive", "--verbose" });
    config.AddExample(new[] { "ClientApp/package.json", "--output-dir", "./client-licenses" });

    config.ValidateExamples();
});

return await app.RunAsync(args);
