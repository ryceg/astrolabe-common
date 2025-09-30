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

| Option                       | Description                                                                                                           | Default             |
| ---------------------------- | --------------------------------------------------------------------------------------------------------------------- | ------------------- |
| `-o`, `--output-dir`         | The directory where license reports will be saved.                                                                    | `./license-reports` |
| `-t`, `--include-transitive` | Includes transitive dependencies in the scan.                                                                         | `false`             |
| `--json`                     | Generate JSON output.                                                                                                 | `false`             |
| `--csv`                      | Generate CSV output.                                                                                                  | `true` (default)    |
| `--xlsx`                     | Generate Excel output.                                                                                                | `false`             |
| `--production`               | (npm/Rush only) Only include production dependencies.                                                                 | `false`             |
| `--development`              | (npm/Rush only) Only include development dependencies.                                                                | `false`             |
| `--exclude-private-packages` | (npm/Rush only) Exclude packages marked as private.                                                                   | `false`             |
| `--nested-search-path`       | A glob pattern for discovering nested `package.json` files (e.g., `**/frontend/**`). Can be specified multiple times. |                     |
| `--allowed-license`          | License identifier to allow (e.g., `MIT`, `Apache-2.0`). Can be specified multiple times. Overrides config file.      |                     |
| `--disallowed-license`       | License identifier to explicitly disallow (e.g., `GPL-3.0`). Can be specified multiple times.                         |                     |
| `--skiplist`                 | Package name to skip during scanning (e.g., `my-internal-package`). Can be specified multiple times.                  |                     |
| `--safelist`                 | Package name to safelist, format: `package-name=reason`. Can be specified multiple times.                             |                     |
| `-i`, `--interactive`        | Enable interactive mode to review and configure problematic packages.                                                 | `false`             |
| `--cache-dir`                | Directory for caching package metadata (useful for CI/CD). If not specified, uses temp directory.                     |                     |
| `--no-cache`                 | Disable caching and fetch all package data fresh from registries.                                                     | `false`             |
| `-v`, `--verbose`            | Enable verbose output for more detailed logging.                                                                      | `false`             |
| `-h`, `--help`               | Show help information.                                                                                                |                     |

### Examples

**Run on the current directory (auto-detects solution or project files):**

```sh
astrolabe-license-check
```

**Scan a specific solution and include transitive dependencies:**

```sh
astrolabe-license-check MySolution.sln --include-transitive
```

**Generate JSON and Excel reports (CSV is generated by default):**

```sh
astrolabe-license-check --json --xlsx
```

**Generate only JSON output:**

```sh
astrolabe-license-check client/package.json --json
```

**Scan only production dependencies for a Rush monorepo:**

```sh
astrolabe-license-check rush.json --production
```

**Discover `package.json` files using a wildcard pattern:**

```sh
astrolabe-license-check --nested-search-path "**/frontend/**/package.json"
```

**Allow only specific licenses via command line:**

```sh
astrolabe-license-check --allowed-license MIT --allowed-license Apache-2.0 --allowed-license BSD-3-Clause
```

**Disallow specific licenses:**

```sh
astrolabe-license-check --disallowed-license GPL-3.0 --disallowed-license AGPL-3.0
```

**Skip and safelist packages:**

```sh
astrolabe-license-check --skiplist my-internal-package --safelist "legacy-package=Approved until Q4 2025"
```

**Use interactive mode to review problematic packages:**

```sh
astrolabe-license-check --interactive
```

This will scan your project and, if problematic licenses are found, prompt you interactively to:

- Safelist specific packages with documented reasons
- Add licenses to the allowed or disallowed lists
- Skip packages from future scans
- View detailed package information
- Automatically save configuration changes to `license-check.json`

## Interactive Mode

Interactive mode (`--interactive` or `-i`) provides a guided experience for handling packages with problematic licenses. When enabled, the tool will:

1. **Scan your project** as normal and identify any packages with non-compliant licenses
2. **Present each problematic package** with detailed information and a menu of options
3. **Guide you through decisions** for each package with context-aware prompts
4. **Save your configuration** to `license-check.json` after review
5. **Optionally re-run** the scan with the updated configuration to verify compliance

## Configuration

For advanced license validation, you can create a `license-check.json` file in the root of your project.

Here is an example configuration:

```json
{
  "allowedLicenses": ["MIT", "Apache-2.0", "BSD-3-Clause", "ISC"],
  "disallowedLicenses": ["GPL-3.0", "AGPL-3.0"],
  "skiplist": ["my-internal-package", "another-ignored-package"],
  "safelist": {
    "some-legacy-package": "Approved for legacy support until Q4. See JIRA-456."
  }
}
```

- `allowedLicenses`: A list of license identifiers that are considered compliant. If a package's license is not in this list, it will be marked as problematic. The tool includes a default list of common permissive licenses if this is not provided.
- `disallowedLicenses`: A list of license identifiers that are explicitly forbidden. Packages with these licenses will always be marked as problematic, even if they appear in `allowedLicenses`.
- `skiplist`: A list of package names to completely exclude from the scan.
- `safelist`: A dictionary of package names that should not cause the build to fail, even if they have a non-allowed license. The value is a string explaining the reason for safelisting.

**Note:** Command-line arguments (`--allowed-license`, `--disallowed-license`, `--skiplist`, `--safelist`) override values from the configuration file.

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

### Caching in CI/CD

To speed up license checks in CI/CD pipelines, use the `--cache-dir` option to persist package metadata between runs. This dramatically reduces API calls to package registries.

**GitHub Actions Example:**

```yaml
- name: Cache License Data
  uses: actions/cache@v3
  with:
    path: .license-cache
    key: license-cache-${{ hashFiles('**/packages.lock.json', '**/package-lock.json') }}
    restore-keys: |
      license-cache-

- name: Run License Check
  run: astrolabe-license-check --cache-dir .license-cache
```

**GitLab CI Example:**

```yaml
license-check:
  cache:
    key: license-cache
    paths:
      - .license-cache/
  script:
    - astrolabe-license-check --cache-dir .license-cache
```

**Azure Pipelines Example:**

```yaml
- task: Cache@2
  inputs:
    key: 'license-cache | "$(Agent.OS)"'
    path: .license-cache
  displayName: Cache license metadata

- script: astrolabe-license-check --cache-dir .license-cache
  displayName: Run license check
```

### Disabling Cache

To force fresh data from registries (useful for troubleshooting or when you suspect stale cache data):

```bash
astrolabe-license-check --no-cache
```

This disables both in-memory and disk caching, ensuring all package metadata is fetched fresh from NuGet and npm registries.

**Example (without caching):**

```yaml
- name: Run License Check
  run: astrolabe-license-check
```
