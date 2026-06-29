#!/usr/bin/env bash
# Packs DataPort, DataPort.Extensions, and DataPort.Extensions.Testing and installs
# them into the local NuGet cache, replacing any existing version.
#
# Usage: ./publish-local.sh <version>
# Example: ./publish-local.sh 0.30.0

set -euo pipefail

VERSION="${1:-}"
if [[ -z "$VERSION" ]]; then
    echo "Usage: $0 <version>"
    echo "Example: $0 0.30.0"
    exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
NUGET_CACHE="$HOME/.nuget/packages"
OUTPUT_DIR="$SCRIPT_DIR/artifacts/local-packages"

PROJECTS=(
    "src/DataPort/DataPort.csproj"
    "src/DataPort.Extensions/DataPort.Extensions.csproj"
    "src/DataPort.Extensions.Testing/DataPort.Extensions.Testing.csproj"
)

# Lowercase package IDs as NuGet stores them in the cache
PACKAGE_IDS=(
    "vicione.suite.dataport"
    "vicione.suite.dataport.extensions"
    "vicione.suite.dataport.extensions.testing"
)

# Clean output dir
rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"

# Restore & Pack
echo "--- Restoring & packing version $VERSION ---"
for project in "${PROJECTS[@]}"; do
    echo "Packing $project..."
    dotnet pack "$SCRIPT_DIR/$project" \
        -p:PackageVersion="$VERSION" \
        -o "$OUTPUT_DIR" \
        -c Release
done

# Remove existing cache entries so the push overwrites them
echo ""
echo "--- Clearing existing cache entries ---"
for id in "${PACKAGE_IDS[@]}"; do
    target="$NUGET_CACHE/$id/$VERSION"
    if [[ -d "$target" ]]; then
        echo "Removing $target"
        rm -rf "$target"
    fi
done

# Push to local cache
echo ""
echo "--- Installing into local NuGet cache ---"
for nupkg in "$OUTPUT_DIR"/*.nupkg; do
    echo "Pushing $(basename "$nupkg")..."
    dotnet nuget push "$nupkg" --source "$NUGET_CACHE"
done

echo ""
echo "Done. Version $VERSION is available in the local NuGet cache."
