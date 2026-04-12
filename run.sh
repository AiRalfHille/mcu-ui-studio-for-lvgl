#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-/tmp/dotnet-cli}"
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE="${DOTNET_SKIP_FIRST_TIME_EXPERIENCE:-1}"
export AVALONIA_TELEMETRY_OPTOUT="${AVALONIA_TELEMETRY_OPTOUT:-1}"

mkdir -p "$DOTNET_CLI_HOME"

cd "$ROOT_DIR"

dotnet run \
  --project src/Ai.McuUiStudio.App/Ai.McuUiStudio.App.csproj \
  -c Debug \
  --disable-build-servers \
  -p:UseSharedCompilation=false
