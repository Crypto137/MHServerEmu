#!/bin/bash

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
BUILD_DIR="$PROJECT_DIR/src/MHServerEmu/bin/x64/Release/net8.0"

if [[ -f "$BUILD_DIR/MHServerEmu.dll" ]]; then
    cd "$BUILD_DIR"
    DOTNET_ROLL_FORWARD=LatestMajor exec dotnet MHServerEmu.dll "$@"
else
    echo "Error: MHServerEmu.dll not found at $BUILD_DIR"
    echo "Build the server first: dotnet build MHServerEmu.sln -c Release"
    exit 1
fi