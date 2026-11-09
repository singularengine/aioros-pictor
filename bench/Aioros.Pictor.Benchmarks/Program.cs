using Aioros.Pictor.Benchmarks;
using BenchmarkDotNet.Running;

if (args is ["selectors", ..])
{
    await HoverRuleBenchmarks.PrintAsync(args.Length > 1 ? args[1] : null);
    return;
}

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
