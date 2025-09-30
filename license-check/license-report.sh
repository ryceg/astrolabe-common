#!/bin/bash

export PATH="$PATH:$HOME/.dotnet/tools"

get_abs_path() {
  (
    cd "$(dirname "$1")"
    echo "$(pwd)/$(basename "$1")"
  )
}

echo "Generating license reports..."

# Function to find rush.json by traversing up from current directory
find_rush_root() {
    local current_dir="$1"
    while [[ "$current_dir" != "/" ]]; do
        if [[ -f "$current_dir/rush.json" ]]; then
            echo "$current_dir"
            return 0
        fi
        current_dir=$(dirname "$current_dir")
    done
    return 1
}


extract_csproj_files() {
    local SLN_FILE="$1"
    grep -o '"[^"]*\.csproj"' "$SLN_FILE" | tr -d '"' | tr '\\' '/' || echo ""
}

process_rush_file() {
    local RUSH_FILE="$1"
    local RUSH_ROOT=$(dirname "$RUSH_FILE")
    echo "Processing Rush root at: $RUSH_ROOT"

    # Save current directory to return to later
    local ORIGINAL_DIR="$(pwd)"
    cd "$RUSH_ROOT" || exit

    # Extract project folders from rush.json
    local PROJECTS=$(grep -o '"projectFolder": "[^"]*"' rush.json | cut -d'"' -f4)

    # Set up the header for Rush projects license report if it doesn't exist
    if [ ! -f "$ORIGINAL_DIR/$RUSH_REPORT" ] || [ ! -s "$ORIGINAL_DIR/$RUSH_REPORT" ]; then
        echo "PackageName,Version,License,Repository,UpdateDate" > "$ORIGINAL_DIR/$RUSH_REPORT"
    fi

    for PROJECT in $PROJECTS; do
        if [ -f "$PROJECT/package.json" ]; then
            echo "Processing Rush project: $PROJECT..."
            # Call process_package_json with an additional parameter to indicate it's from Rush
            process_package_json "$RUSH_ROOT/$PROJECT/package.json" "$ORIGINAL_DIR/$RUSH_REPORT" "rush"
        fi
    done

    # Return to original directory
    cd "$ORIGINAL_DIR" || exit
}


process_package_json() {
    local PKG_FILE="$1"
    local PKG_DIR=$(dirname "$PKG_FILE")
    local OUTPUT_FILE="$2"
    local SOURCE="$3"  # Optional parameter: "rush" if called from process_rush_file

    echo "Processing package.json at: $PKG_DIR"

    # Save current directory to return to later
    local ORIGINAL_DIR="$(pwd)"

    # Convert OUTPUT_DIR to absolute path if it's relative
    local ABS_OUTPUT_DIR
    if [[ "$OUTPUT_DIR" = /* ]]; then
        ABS_OUTPUT_DIR="$OUTPUT_DIR"
    else
        ABS_OUTPUT_DIR="$ORIGINAL_DIR/$OUTPUT_DIR"
    fi

    cd "$PKG_DIR" || exit

    # Check if this is part of a Rush project and not explicitly processed as Rush
    if [ "$SOURCE" != "rush" ] && find_rush_root "$PKG_DIR" > /dev/null; then
        echo "This package.json appears to be part of a Rush project. Skipping direct processing."
    else
        # Install license-checker if needed
        if ! command -v license-checker &> /dev/null; then
            echo "license-checker not found. Installing..."
            npm install -g license-checker
        fi

        local LICENSE_CHECKER_ARGS="--json"
        if [ "$INCLUDE_PROD" = true ] && [ "$INCLUDE_DEV" = false ]; then
            LICENSE_CHECKER_ARGS="$LICENSE_CHECKER_ARGS --production"
        elif [ "$INCLUDE_PROD" = false ] && [ "$INCLUDE_DEV" = true ]; then
            LICENSE_CHECKER_ARGS="$LICENSE_CHECKER_ARGS --development"
        fi

        # Use different approaches based on source
        if [ "$SOURCE" = "rush" ]; then
            # For Rush projects, also use license-checker but append to the Rush report
            echo "Processing as part of Rush project..."

            # Use Node.js to parse JSON (no external dependencies needed)
            if [ "$INCLUDE_UPDATE_DATES" = true ]; then
                # Get package update dates from npm view (slower but includes dates)
                echo "Fetching update dates from npm registry (this may take a while)..."
                license-checker $LICENSE_CHECKER_ARGS | node -e "
                  const data = JSON.parse(require('fs').readFileSync(0, 'utf-8'));
                  for (const [key, value] of Object.entries(data)) {
                    const match = key.match(/^(.+)@([0-9]+\.[0-9]+\.[0-9]+.*)$/);
                    const name = match ? match[1] : key;
                    const version = match ? match[2] : 'Unknown';
                    const licenses = value.licenses || 'Unknown';
                    const repository = value.repository || 'Unknown';
                    console.log(JSON.stringify({name, version, licenses, repository}));
                  }
                " | while IFS= read -r line; do
                    if [ -n "$line" ]; then
                        pkg_name=$(echo "$line" | node -pe "JSON.parse(require('fs').readFileSync(0)).name")
                        pkg_version=$(echo "$line" | node -pe "JSON.parse(require('fs').readFileSync(0)).version")
                        pkg_licenses=$(echo "$line" | node -pe "JSON.parse(require('fs').readFileSync(0)).licenses")
                        pkg_repo=$(echo "$line" | node -pe "JSON.parse(require('fs').readFileSync(0)).repository")

                        # Fetch update date from npm registry
                        update_date=$(npm view "$pkg_name@$pkg_version" time.modified 2>/dev/null || echo "Unknown")

                        # CSV escape and output
                        printf '"%s","%s","%s","%s","%s"\n' "$pkg_name" "$pkg_version" "$pkg_licenses" "$pkg_repo" "$update_date" >> "$OUTPUT_FILE"
                    fi
                done
            else
                # Fast mode without update dates - use Node.js to parse and format
                license-checker $LICENSE_CHECKER_ARGS | node -e "
                  const data = JSON.parse(require('fs').readFileSync(0, 'utf-8'));
                  for (const [key, value] of Object.entries(data)) {
                    const match = key.match(/^(.+)@([0-9]+\.[0-9]+\.[0-9]+.*)$/);
                    const name = match ? match[1] : key;
                    const version = match ? match[2] : 'Unknown';
                    const licenses = value.licenses || 'Unknown';
                    const repository = value.repository || 'Unknown';
                    // CSV format with proper escaping
                    const escape = (s) => '\"' + String(s).replace(/\"/g, '\"\"') + '\"';
                    console.log([escape(name), escape(version), escape(licenses), escape(repository), '\"N/A\"'].join(','));
                  }
                " >> "$OUTPUT_FILE"
            fi
        else
            # For standalone projects, use license-checker with direct output
            echo "Processing as standalone npm project..."

            # Generate a unique filename for this package
            local PKG_NAME=$(basename "$PKG_DIR")
            local NPM_REPORT_FOR_PKG="$ABS_OUTPUT_DIR/npm-licenses-$PKG_NAME.json"

            # If no output file is specified, use the default
            if [ -z "$OUTPUT_FILE" ]; then
                OUTPUT_FILE="$NPM_REPORT_FOR_PKG"
            fi

            # Use --production to only check production dependencies (faster)
            echo "Running license-checker (this may take a moment for large projects)..."
            license-checker $LICENSE_CHECKER_ARGS --out "$OUTPUT_FILE"
            echo "NPM license report generated at $OUTPUT_FILE"
        fi
    fi

    # Return to original directory
    cd "$ORIGINAL_DIR" || exit
}

process_csproj_file() {
    local CSPROJ_PATH="$1"
    local CSPROJ_DIR="$(cd "$(dirname "$CSPROJ_PATH")" && pwd)"
    local CSPROJ_FILENAME=$(basename "$CSPROJ_PATH")
    local SKIP_NUGET_SCAN="${2:-false}"  # Optional parameter to skip nuget scan

    echo "Processing csproj file: $CSPROJ_PATH"

    # Process .NET dependencies with nuget-license unless told to skip
    if [ "$SKIP_NUGET_SCAN" != "true" ]; then
        nuget-license --input "$CSPROJ_PATH" --include-transitive --output JsonPretty --file-output "$NUGET_REPORT"
        echo "NuGet license report generated at $NUGET_REPORT"
    fi

    # Only check for rush.json if no rush.json or package.json was explicitly provided
    if [ "$CHECK_CSPROJ_DIRS" = true ]; then
        echo "Checking for rush.json in: $CSPROJ_DIR"

        if [ -f "$CSPROJ_DIR/rush.json" ]; then
            echo "Found rush.json in $CSPROJ_DIR"
            process_rush_file "$CSPROJ_DIR/rush.json"
        else
            echo "No rush.json found in $CSPROJ_DIR"
        fi

        # Also check for package.json
        if [ -f "$CSPROJ_DIR/package.json" ]; then
            echo "Found package.json in $CSPROJ_DIR"
            process_package_json "$CSPROJ_DIR/package.json"
        fi
    else
        echo "Skipping rush.json check in .csproj directory (explicit files provided)"
    fi
}


process_solution_file() {
    local SOLUTION_PATH="$1"
    local SOLUTION_DIR="$(cd "$(dirname "$SOLUTION_PATH")" && pwd)"
    local SLN_FILENAME=$(basename "$SOLUTION_PATH")

    echo "Processing solution file: $SOLUTION_PATH"

    # Process .NET dependencies with nuget-license at solution level for efficiency
    nuget-license --input "$SOLUTION_PATH" --include-transitive --output JsonPretty --file-output "$NUGET_REPORT"
    echo "NuGet license report generated at $NUGET_REPORT"

    # Only check for rush.json if no rush.json or package.json was explicitly provided
    if [ "$CHECK_CSPROJ_DIRS" = true ]; then
        echo "Extracting .csproj files from solution..."

        # Extract all .csproj files from the solution
        local CSPROJ_PATHS=$(extract_csproj_files "$SOLUTION_PATH")

        if [ -n "$CSPROJ_PATHS" ]; then
            echo "Found the following .csproj files:"
            echo "$CSPROJ_PATHS"

            # Process each directory containing a .csproj
            for CSPROJ in $CSPROJ_PATHS; do
                # Get the absolute path to the .csproj
                local CSPROJ_ABS_PATH
                if [[ "$CSPROJ" == /* ]]; then
                    CSPROJ_ABS_PATH="$CSPROJ"
                else
                    CSPROJ_ABS_PATH="$SOLUTION_DIR/$CSPROJ"
                fi

                # Process the csproj file but skip the nuget scan since we already did it at solution level
                process_csproj_file "$CSPROJ_ABS_PATH" "true"
            done
        else
            echo "No .csproj files found in the solution."
        fi
    else
        echo "Skipping rush.json check in .csproj directories (explicit files provided)"
    fi
}

# Check if nuget-license is installed
if ! command -v nuget-license &> /dev/null; then
    echo "nuget-license not found. Installing..."
    dotnet tool install --global nuget-license
fi

# Set up the header for Rush projects license report
echo "PackageName,Version,License,Repository,UpdateDate" > "$RUSH_REPORT"

# Track whether we've already processed package.json in current directory
PROCESSED_CURRENT_PKG=false

# Flag to check if we need to search for rush.json in .csproj directories
CHECK_CSPROJ_DIRS=true

# Parse command line arguments
GENERATE_CSV=false
GENERATE_EXCEL=false
GENERATE_JSON=false
ANY_FORMAT_SPECIFIED=false
INCLUDE_PROD=false
INCLUDE_DEV=false
ANY_DEP_TYPE_SPECIFIED=false
INCLUDE_UPDATE_DATES=false
FILES=()
OUTPUT_DIR="./license-reports" # Default output directory

while [[ $# -gt 0 ]]; do
  arg="$1"
  case $arg in
    --csv)
      GENERATE_CSV=true
      ANY_FORMAT_SPECIFIED=true
      shift # past argument
      ;;
    --excel)
      GENERATE_EXCEL=true
      ANY_FORMAT_SPECIFIED=true
      shift # past argument
      ;;
    --json)
      GENERATE_JSON=true
      ANY_FORMAT_SPECIFIED=true
      shift # past argument
      ;;
    --production)
      INCLUDE_PROD=true
      ANY_DEP_TYPE_SPECIFIED=true
      shift # past argument
      ;;
    --development)
      INCLUDE_DEV=true
      ANY_DEP_TYPE_SPECIFIED=true
      shift # past argument
      ;;
    --update-dates)
      INCLUDE_UPDATE_DATES=true
      shift # past argument
      ;;
    --output)
      OUTPUT_DIR="$2"
      shift # past argument
      shift # past value
      ;;
    *)
      FILES+=("$1") # save it in an array for later
      shift # past argument
      ;;
  esac
done

# Default to CSV if no other format is specified
if [ "$ANY_FORMAT_SPECIFIED" = false ]; then
    GENERATE_CSV=true
fi

# Default to both prod and dev if no dependency type is specified
if [ "$ANY_DEP_TYPE_SPECIFIED" = false ]; then
    INCLUDE_PROD=true
    INCLUDE_DEV=true
fi

# Define output files based on the output directory
NUGET_REPORT="$OUTPUT_DIR/nuget-licenses.json"
NPM_REPORT="$OUTPUT_DIR/npm-licenses.json"
RUSH_REPORT="$OUTPUT_DIR/rush-dependencies.csv"

# Create output directory if it doesn't exist
mkdir -p "$OUTPUT_DIR"

# Check if any rush.json or package.json files are explicitly provided
for arg in "${FILES[@]}"; do
    if [[ "$arg" == *rush.json ]] || [[ "$arg" == *package.json ]]; then
        CHECK_CSPROJ_DIRS=false
        break
    fi
done

# Process file arguments
if [ ${#FILES[@]} -eq 0 ]; then
    echo "No files specified. Searching for .sln files in current directory..."
    SLN_FILES=(*.sln)
    if [ ${#SLN_FILES[@]} -eq 0 ] || [ "${SLN_FILES[0]}" == "*.sln" ]; then
        echo "No .sln files found in current directory."
    else
        echo "Found solution file: ${SLN_FILES[0]}"
        process_solution_file "${SLN_FILES[0]}"
    fi
else
    # Process each file argument
    for arg in "${FILES[@]}"; do
        if [[ "$arg" == *rush.json ]]; then
            echo "Processing Rush file: $arg"
            process_rush_file "$arg"
        elif [[ "$arg" == *.sln ]]; then
            echo "Processing Solution file: $arg"
            process_solution_file "$arg"
        elif [[ "$arg" == *.csproj ]]; then
            echo "Processing Project file: $arg"
            process_csproj_file "$arg"
        elif [[ "$arg" == *package.json ]]; then
            echo "Processing package.json file: $arg"
            process_package_json "$arg"
            # Check if this is the package.json in current directory
            if [[ "$(get_abs_path "$arg")" == "$(get_abs_path "./package.json")" ]]; then
                PROCESSED_CURRENT_PKG=true
            fi
        else
            echo "Unrecognized file type: $arg"
            echo "Please provide .sln, .csproj, rush.json, or package.json files."
        fi
    done
fi

# Check and run standard license-checker for any regular npm projects in current dir
# Only if we've already processed it via command line arguments
if [ "$PROCESSED_CURRENT_PKG" = false ] && [ -f "package.json" ] && ! find_rush_root "$(pwd)" > /dev/null; then
    echo "Processing package.json in current directory..."
    if ! command -v license-checker &> /dev/null; then
        echo "license-checker not found. Installing..."
        npm install -g license-checker
    fi

    local LICENSE_CHECKER_ARGS="--json"
    if [ "$INCLUDE_PROD" = true ] && [ "$INCLUDE_DEV" = false ]; then
        LICENSE_CHECKER_ARGS="$LICENSE_CHECKER_ARGS --production"
    elif [ "$INCLUDE_PROD" = false ] && [ "$INCLUDE_DEV" = true ]; then
        LICENSE_CHECKER_ARGS="$LICENSE_CHECKER_ARGS --development"
    fi

    if [ "$INCLUDE_PROD" = true ] || [ "$INCLUDE_DEV" = true ]; then
        license-checker $LICENSE_CHECKER_ARGS --out "$NPM_REPORT"
        echo "NPM license report generated at $NPM_REPORT"
    fi
else
    echo "Skipping package.json in current directory (already processed or part of Rush project)."
fi

echo "License reports generated in $OUTPUT_DIR"

# Generate CSV report if requested
if [ "$GENERATE_CSV" = true ]; then
    echo "Generating consolidated CSV report..."

    CONSOLIDATED_CSV="$OUTPUT_DIR/license-report.csv"

    # Create header
    echo "Source,PackageName,Version,License,Repository,UpdateDate" > "$CONSOLIDATED_CSV"

    # Process NuGet packages if the file exists
    if [ -f "$NUGET_REPORT" ]; then
        echo "Processing NuGet packages for CSV..."
        node -e "
          const data = JSON.parse(require('fs').readFileSync('$NUGET_REPORT', 'utf-8'));
          const escape = (s) => '\"' + String(s || 'Unknown').replace(/\"/g, '\"\"') + '\"';
          data.forEach(pkg => {
            console.log(['NuGet', pkg.PackageName, pkg.PackageVersion, pkg.License, pkg.PackageProjectUrl || 'Unknown', 'N/A'].map(escape).join(','));
          });
        " >> "$CONSOLIDATED_CSV"
    fi

    # Process Rush dependencies if the file exists
    if [ -f "$RUSH_REPORT" ] && [ -s "$RUSH_REPORT" ]; then
        echo "Processing Rush/NPM packages for CSV..."
        # Skip the header and prepend "NPM" to each line
        tail -n +2 "$RUSH_REPORT" | while IFS= read -r line; do
            echo "NPM,$line" >> "$CONSOLIDATED_CSV"
        done
    fi

    # Process standalone NPM reports if they exist
    for npm_report in "$OUTPUT_DIR"/npm-licenses-*.json; do
        if [ -f "$npm_report" ]; then
            echo "Processing standalone NPM packages for CSV from $(basename "$npm_report")..."
            node -e "
              const data = JSON.parse(require('fs').readFileSync('$npm_report', 'utf-8'));
              const escape = (s) => '\"' + String(s || 'Unknown').replace(/\"/g, '\"\"') + '\"';
              for (const [key, value] of Object.entries(data)) {
                const match = key.match(/^(.+)@([0-9]+\.[0-9]+\.[0-9]+.*)$/);
                const name = match ? match[1] : key;
                const version = match ? match[2] : 'Unknown';
                const licenses = value.licenses || 'Unknown';
                const repository = value.repository || 'Unknown';
                console.log(['NPM', name, version, licenses, repository, 'N/A'].map(escape).join(','));
              }
            " >> "$CONSOLIDATED_CSV"
        fi
    done

    echo "Consolidated CSV report generated at $CONSOLIDATED_CSV"
fi

# Generate Excel report if requested
if [ "$GENERATE_EXCEL" = true ]; then
    echo "Generating Excel report..."

    if ! command -v pipx &> /dev/null; then
        echo "pipx not found. Installing..."
        if ! command -v python3 &> /dev/null; then
            echo "Python 3 not found. Please install Python 3 to generate Excel reports."
            exit 1
        fi

        sudo apt-get update && sudo apt-get install -y pipx
        python3 -m pipx ensurepath

        if [ -f ~/.bashrc ]; then
            source ~/.bashrc
        fi
    fi

    pipx run --spec pandas --spec openpyxl python "$(dirname "$0")/generate-reports.py"
fi

# Generate JSON report if requested
if [ "$GENERATE_JSON" = true ]; then
    echo "Generating consolidated JSON report..."

    CONSOLIDATED_JSON="$OUTPUT_DIR/license-report.json"

    # Start with an empty JSON array
    echo "[]" > "$CONSOLIDATED_JSON"

    # Process NuGet packages if the file exists
    if [ -f "$NUGET_REPORT" ]; then
        echo "Processing NuGet packages for JSON..."
        # Add a "Source" field to each object and merge it into the consolidated file
        node -e "
          const nuget = JSON.parse(require('fs').readFileSync('$NUGET_REPORT', 'utf-8'));
          const consolidated = JSON.parse(require('fs').readFileSync('$CONSOLIDATED_JSON', 'utf-8'));
          const nugetWithSource = nuget.map(pkg => ({...pkg, Source: 'NuGet'}));
          const merged = consolidated.concat(nugetWithSource);
          require('fs').writeFileSync('$CONSOLIDATED_JSON', JSON.stringify(merged, null, 2));
        "
    fi

    # Process Rush dependencies if the file exists
    if [ -f "$RUSH_REPORT" ] && [ -s "$RUSH_REPORT" ]; then
        echo "Processing Rush/NPM packages for JSON..."
        # Convert CSV to JSON and merge
        node -e "
          const csv = require('fs').readFileSync('$RUSH_REPORT', 'utf-8');
          const lines = csv.trim().split('\n');
          const header = lines.shift().split(',');
          const rush = lines.map(line => {
            const values = line.split(',');
            const obj = {};
            header.forEach((h, i) => {
              obj[h] = values[i].replace(/\"/g, '');
            });
            return {...obj, Source: 'NPM'};
          });
          const consolidated = JSON.parse(require('fs').readFileSync('$CONSOLIDATED_JSON', 'utf-8'));
          const merged = consolidated.concat(rush);
          require('fs').writeFileSync('$CONSOLIDATED_JSON', JSON.stringify(merged, null, 2));
        "
    fi

    echo "Consolidated JSON report generated at $CONSOLIDATED_JSON"
fi
