using Aioros.Pictor.Demo;
using Aioros.Pictor.Svg;

var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
var mode = args.Length > 0 ? args[0] : "render";
var output = args.Length > 1 ? Path.GetFullPath(args[1]) : Path.Combine(root, "samples/Demo/out");
var only = Environment.GetEnvironmentVariable("PICTOR_EXAMPLES") is { Length: > 0 } names ? names.Split(',') : null;
var examples = (await RechartsExamples.LoadAsync(Path.Combine(root, "tools/recharts/examples"), CancellationToken.None))
    .Where(example => only is null || only.Contains(example.Name)).ToList();
Directory.CreateDirectory(Path.Combine(output, "pictor"));
switch (mode)
{
    case "render":
        await Render.WriteAsync(examples, output);
        break;
    case "animated":
        await Render.AnimatedAsync(examples, output);
        break;
    case "accessible":
        await Render.AccessibleAsync(examples, output);
        break;
    case "updates":
        await Render.UpdatesAsync(root, output);
        break;
    case "profile":
        await Profile.RunAsync(examples);
        break;
    case "desktop":
        await Desktop.WriteAsync(examples, root, output);
        break;
    case "desktop-dark":
        await DarkDesktop.WriteAsync(examples, output);
        break;
    case "page":
        await DemoPage.WriteAsync(examples, root, output, args.Length > 2 ? args[2] : "local");
        break;
    default:
        await Console.Error.WriteLineAsync("usage: Demo render|animated|profile|desktop|desktop-dark|page [out] [commit]");
        return 1;
}

return 0;

