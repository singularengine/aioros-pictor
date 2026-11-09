#!/usr/bin/env bash
# Usage: samples/Demo/run.sh [out]
# Draws every Recharts example with Pictor (web and desktop), compares each with Recharts' own render, times both
# sides and writes samples/Demo/out/index.html; the raw results go to bench/results/demo/<date>-<commit>.json.
set -euo pipefail
here="$(cd "$(dirname "$0")" && pwd)"
root="$(cd "$here/../.." && pwd)"
out="${1:-$here/out}"
commit="$(git -C "$root" rev-parse --short HEAD)"

if [ ! -d "$root/tools/recharts/node_modules/recharts" ]; then
  (cd "$root/tools/recharts" && npm ci --silent)
fi

dotnet build "$here/Demo.csproj" -c Release -m:4 --nologo -v quiet
demo="$here/bin/Release/net10.0/Demo.dll"
dotnet "$demo" render "$out"
node "$here/compare.mjs" "$out"
dotnet "$demo" animated "$out"
node "$here/animations.mjs" "$out"
dotnet "$demo" desktop "$out"
dotnet "$demo" page "$out" "$commit"

mkdir -p "$root/bench/results/demo"
cp "$out/results.json" "$root/bench/results/demo/$(date -u +%Y-%m-%d)-$commit.json"
echo "raw results in bench/results/demo/$(date -u +%Y-%m-%d)-$commit.json"
