#!/usr/bin/env bash
# Build script for static hosts that do not ship the .NET SDK (used by Vercel).
set -euo pipefail

curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
bash /tmp/dotnet-install.sh --channel 8.0 --install-dir "$HOME/.dotnet"

export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
"$HOME/.dotnet/dotnet" publish src/StoreAdmin -c Release -o output
