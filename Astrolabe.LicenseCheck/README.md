# Astrolabe.LicenseCheck

A cross-platform command-line tool for checking licenses of dependencies in .NET and npm projects. It generates reports in various formats and can be integrated into a CI/CD pipeline to enforce license compliance.

## Features

- Scans .NET solution (`.sln`) and project (`.csproj`) files.
- Scans npm `package.json` and Rush monorepo `rush.json` files.
- Generates reports in JSON, CSV, and Excel formats.
- Performs package age analysis to identify very new or outdated packages.
- Configurable license validation with allowed lists, safelists, and skiplists.
- Provides a non-zero exit code for pipeline integration when problematic licenses are found.

## Installation

Install the tool globally using the .NET CLI:

```sh
dotnet tool install --global Astrolabe.LicenseCheck
```

## Usage

You can run the tool by name from your terminal.

```sh
astrolabe-license-check [files] [options]
```

If no input files are specified, the tool will automatically search the current directory for `.sln` files, and if none are found, it will search for `.csproj` files.

### Arguments

- `[files]` (optional): One or more input files to scan. Can be `.sln`, `.csproj`, `rush.json`, or `package.json`.

### Options

| Option                       | Description                                                                 | Default           |
| ---------------------------- | --------------------------------------------------------------------------- | ----------------- |
| `-o`, `--output-dir`         | The directory where license reports will be saved.                          | `./license-reports` |
| `-t`, `--include-transitive` | Includes transitive dependencies in the scan.                               | `false`           |
| `--format`                   | The output format for the report (`Json`, `Csv`, `Excel`, `All`).           | `All`             |
| `--production`               | (npm/Rush only) Only include production dependencies.                       | `false`           |
| `--development`              | (npm/Rush only) Only include development dependencies.                      | `false`           |
| `--exclude-private-packages` | (npm/Rush only) Exclude packages marked as private.                         | `false`           |
| `--nested-search-path`       | A glob pattern for discovering nested `package.json` files (e.g., `**/frontend/**`). Can be specified multiple times. | `**/ClientApp/sites/**/package.json` |
| `-v`, `--verbose`            | Enable verbose output for more detailed logging.                            | `false`           |
| `-h`, `--help`               | Show help information.                                                      |                   |

### Examples

**Run on the current directory (auto-detects solution or project files):**
```sh
astrolabe-license-check
```

**Scan a specific solution and include transitive dependencies:**
```sh
astrolabe-license-check MySolution.sln --include-transitive
```

**Generate a CSV report for an npm project:**
```sh
astrolabe-license-check client/package.json --format Csv
```

**Scan only production dependencies for a Rush monorepo:**
```sh
astrolabe-license-check rush.json --production
```

**Discover `package.json` files using a wildcard pattern:**
```sh
astrolabe-license-check --nested-search-path "**/frontend/**/package.json"
```

## Configuration

For advanced license validation, you can create a `license-check.json` file in the root of your project.

Here is an example configuration:

```json
{
  "allowedLicenses": [
    "MIT",
    "Apache-2.0",
    "BSD-3-Clause",
    "ISC"
  ],
  "skiplist": [
    "my-internal-package",
    "another-ignored-package"
  ],
  "safelist": {
    "some-legacy-package": "Approved for legacy support until Q4. See JIRA-456."
  }
}
```

- `allowedLicenses`: A list of license identifiers that are considered compliant. If a package's license is not in this list, it will be marked as problematic. The tool includes a default list of common permissive licenses if this is not provided.
- `skiplist`: A list of package names to completely exclude from the scan.
- `safelist`: A dictionary of package names that should not cause the build to fail, even if they have a non-allowed license. The value is a string explaining the reason for safelisting.

## Output Reports

The tool generates reports in the specified output directory.

- `nuget-licenses.json`: A JSON report for all .NET packages.
- `rush-dependencies.csv`: A CSV report for all npm/Rush packages.
- `license-report.xlsx`: An Excel workbook containing sheets for both .NET and npm/Rush dependencies.

The reports include the following columns:
- `PackageId` / `PackageName`
- `PackageVersion` / `Version`
- `License`
- `Repository` / `Project URL`
- `PublishDate`: The date the package version was published.
- `AgeStatus`: The age of the package, categorized as `Fresh`, `Normal`, `Stale`, or `Outdated`.
- `IsProblematic`: `true` if the package's license is not in the `allowedLicenses` list.
- `IsSafelisted`: `true` if the package is in the `safelist`.
- `ProblemReason`: The reason the package is considered problematic or why it was safelisted.

## CI/CD Integration

The tool is designed for use in CI/CD pipelines. It will exit with a non-zero exit code (specifically, `2`) if it finds any packages that are marked as `Problematic` but are not `Safelisted`. You can use this exit code to fail your build or pipeline step.

**Example (generic CI script):**
```yaml
- name: Run License Check
  run: astrolabe-license-check
```
