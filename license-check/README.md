# License Report Tool

A tool to generate comprehensive license reports for C# and Rush projects.

## Features

- Scans dependencies from multiple project types:
  - .NET projects (via `.sln` and `.csproj` files)
  - Node.js projects (via `package.json`)
  - Rush.js monorepos (via `rush.json`)
- Generates consolidated reports in CSV, JSON, and Excel formats.
- Filters dependencies by type (production, development).
- Includes package update dates from npm registry for tracking new packages.
- Handles transitive dependencies.

## Requirements

- .NET SDK (for `nuget-license` tool)
- Node.js and npm (for `license-checker` and JSON parsing)
- Python 3 (optional, only required for Excel report generation with `--excel` flag)

## Usage

The script can be run without arguments, in which case it searches for `.sln` files in the current directory. You can also provide one or more `.sln`, `.csproj`, `rush.json`, or `package.json` files as arguments.

```bash
# Run with a solution file
./license-report.sh path/to/your-project.sln

# Run with a mix of project files
./license-report.sh path/to/project.sln path/to/other/package.json
```

### Options

You can customize the output format and the types of dependencies to include.

**Output Formats:**

-   `--csv`: Generate a consolidated CSV report.
-   `--json`: Generate a consolidated JSON report.
-   `--excel`: Generate an Excel report (requires Python 3).

If no format flag is specified, the script defaults to generating a **CSV** report. You can specify multiple formats at once.

```bash
# Generate both a CSV and a JSON report
./license-report.sh --csv --json
```

**Dependency Types:**

-   `--production`: Include only production dependencies.
-   `--development`: Include only development dependencies.

If no dependency type flag is specified, the script defaults to including **both production and development** dependencies.

```bash
# Include only production dependencies
./license-report.sh --production
```

**Other Options:**

-   `--output <directory>`: Specify a custom output directory for the reports. Defaults to `./license-reports`.
-   `--update-dates`: Include package update dates from the npm registry. This is slower as it requires a network call for each npm package.

### Output

License reports are saved in the specified output directory (`./license-reports/` by default). The script generates intermediate files (like `nuget-licenses.json`) and a final consolidated report in the format(s) you specified (e.g., `license-report.csv`, `license-report.json`).