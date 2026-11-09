#!/usr/bin/env bash
# Usage: bench/run.sh [micro|demo|recharts|all] [BenchmarkDotNet filter]
# Runs Pictor's micro-benchmarks (BenchmarkDotNet), Recharts' measurements (tools/recharts: server render in Node,
# client render in Chromium) and the demo (samples/Demo/run.sh), each pinned to BENCH_CPUS (default: the first four
# CPUs this process may use), after the 1-minute load average falls below IDLE_LOAD (default 4) or WAIT_MINUTES
# (default 20) pass. Raw results go to bench/results.
set -euo pipefail
here="$(cd "$(dirname "$0")" && pwd)"
root="$(cd "$here/.." && pwd)"
profile="${1:-all}"
filter="${2:-*}"
commit="$(git -C "$root" rev-parse --short HEAD)"
day="$(date -u +%Y-%m-%d)"
allowed="$(taskset -pc $$ | sed 's/.*: //')"
first="${allowed%%[-,]*}"
cpus="${BENCH_CPUS:-$first-$((first + 3))}"
idle="${IDLE_LOAD:-4}"

wait_idle() {
  local deadline=$(( $(date +%s) + ${WAIT_MINUTES:-20} * 60 ))
  while awk -v idle="$idle" '{ exit !($1 >= idle) }' /proc/loadavg && [ "$(date +%s)" -lt "$deadline" ]; do
    echo "load $(cut -d' ' -f1-3 /proc/loadavg), waiting for < $idle"
    sleep 30
  done
  echo "load $(cut -d' ' -f1-3 /proc/loadavg) on CPUs $cpus"
}

# The lab measures the 42 reference examples of iteration 3 unless PICTOR_EXAMPLES names others (a full catalog run
# takes longer than the runner's one-hour job limit).
export PICTOR_EXAMPLES="${PICTOR_EXAMPLES:-$(paste -sd, "$here/reference-examples.txt")}"
if [ ! -d "$root/tools/recharts/node_modules/recharts" ]; then
  (cd "$root/tools/recharts" && npm ci --silent)
fi

if [ "$profile" = micro ] || [ "$profile" = all ]; then
  out="$root/bench/results/micro/$day-$commit"
  mkdir -p "$out"
  dotnet build "$root/bench/Aioros.Pictor.Benchmarks" -c Release --nologo -v quiet
  wait_idle
  uptime > "$out/load-before.txt"
  (cd "$out" && taskset -c "$cpus" dotnet "$root/bench/Aioros.Pictor.Benchmarks/bin/Release/net10.0/Aioros.Pictor.Benchmarks.dll" \
    --filter "$filter" --exporters json --artifacts "$out/pictor")
  uptime > "$out/load-after.txt"
  echo "micro results in bench/results/micro/$day-$commit"
fi

if [ "$profile" = recharts ] || [ "$profile" = demo ] || [ "$profile" = all ]; then
  wait_idle
  (cd "$root/tools/recharts" && node render.mjs ${PICTOR_EXAMPLES//,/ } > /dev/null && node screenshot.mjs ${PICTOR_EXAMPLES//,/ } > /dev/null \
    && taskset -c "$cpus" node bench.mjs && taskset -c "$cpus" node bench-client.mjs)
  mkdir -p "$root/bench/results/recharts/$day-$commit"
  cp "$root"/tools/recharts/bench-results/*.json "$root/bench/results/recharts/$day-$commit/"
fi

if [ "$profile" = demo ] || [ "$profile" = all ]; then
  wait_idle
  taskset -c "$cpus" "$root/samples/Demo/run.sh"
fi
