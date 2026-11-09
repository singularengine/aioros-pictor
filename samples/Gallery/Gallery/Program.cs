using Gallery.Analytics;
using Gallery.Models;

var clock = Environment.GetEnvironmentVariable("GALLERY_CLOCK") is { Length: > 0 } start
    ? new Gallery.Clock.SteppedClock(DateTimeOffset.Parse(start, System.Globalization.CultureInfo.InvariantCulture))
    : null;
var builder = AiorosApplication.CreateBuilder(args);
builder.Services.AddRequestLocalization(Gallery.Landing.Languages.Configure);
builder.Services.AddSingleton(clock ?? TimeProvider.System);
builder.Services.AddSingleton<Aioros.Pages.IPrecompileSource, Gallery.Compare.CompareSamples>();
builder.Services.AddSingleton<Aioros.Pages.IPrecompileSource, Gallery.Landing.LandingUrls>();
builder.AddServerFunction<ILiveFunctions, LiveFunctions>();
builder.AddServerFunction<IAnalyticsFunctions, AnalyticsFunctions>();
builder.Server(services =>
{
    services.AddSingleton<LiveFeed>().AddSingleton(new TrafficModel(2026)).AddSingleton<AnalyticsFeed>();
    if (clock is null)
    {
        services.AddHostedService<LiveTicker>().AddHostedService<AnalyticsTicker>();
    }
    else
    {
        services.AddSingleton(clock).AddSingleton<Gallery.Clock.ManualTicker>().AddHostedService(provider => provider.GetRequiredService<Gallery.Clock.ManualTicker>());
    }
});
var app = builder.Build();
await Gallery.Compare.ReferenceExamples.LoadAsync(CancellationToken.None);
if (clock is not null)
{
    app.MapPost("/test/tick", async (Gallery.Clock.ManualTicker ticker, CancellationToken cancellationToken) =>
    {
        await ticker.TickAsync(cancellationToken);
        return Results.NoContent();
    });
}

Gallery.Landing.Languages.MapRoot(app);
app.MapAioros();
app.MapGet("/reference/{name}.png", (string name) => Gallery.Compare.ReferenceExamples.Find(name) is not null && File.Exists(Gallery.Compare.ReferenceExamples.Png(name))
    ? Results.File(Gallery.Compare.ReferenceExamples.Png(name), "image/png")
    : Results.NotFound());
return app.RunAioros();
