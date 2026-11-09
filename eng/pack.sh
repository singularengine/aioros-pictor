#!/usr/bin/env bash
# Usage: eng/pack.sh [version suffix]
# Packs Aioros.Pictor and Aioros.Pictor.Avalonia (with symbols) into artifacts/packages.
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
out="$root/artifacts/packages"
suffix="${1:-}"
mkdir -p "$out"
args=(-c Release -o "$out" --nologo -v quiet)
if [ -n "$suffix" ]; then args+=("-p:VersionSuffix=$suffix"); fi
for project in Aioros.Pictor Aioros.Pictor.Avalonia; do
  if [ -f "$root/src/$project/$project.csproj" ]; then
    dotnet pack "$root/src/$project/$project.csproj" "${args[@]}"
  fi
done
ls -1 "$out"
