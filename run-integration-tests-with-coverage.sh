#!/usr/bin/env bash
set -euo pipefail

# Move to the script directory (solution root)
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

echo "==> Cleaning previous test and coverage outputs..."
rm -rf ./TestResults ./CoverageReport

# Add dotnet global tools to PATH if not already present
if [ -d "$HOME/.dotnet/tools" ]; then
    export PATH="$PATH:$HOME/.dotnet/tools"
fi

# Ensure reportgenerator is installed
if ! command -v reportgenerator &> /dev/null; then
    echo "==> 'reportgenerator' not found. Installing dotnet-reportgenerator-globaltool..."
    dotnet tool install -g dotnet-reportgenerator-globaltool || true
fi

echo "==> Running integration tests and collecting code coverage..."
dotnet test BionicSquare.IntegrationTests/BionicSquare.IntegrationTests.csproj \
    --settings coverlet.runsettings \
    --collect:"XPlat Code Coverage" \
    --results-directory ./TestResults

echo "==> Generating HTML coverage report..."
reportgenerator \
    -reports:"./TestResults/**/coverage.cobertura.xml" \
    -targetdir:"./CoverageReport" \
    -reporttypes:Html

echo ""
echo "==> Integration tests coverage report successfully generated at:"
echo "    file://$SCRIPT_DIR/CoverageReport/index.html"
