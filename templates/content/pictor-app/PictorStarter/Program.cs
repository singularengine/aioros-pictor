using PictorStarter.Analytics;

var builder = AiorosApplication.CreateBuilder(args);
builder.Services.AddSingleton(TimeProvider.System);
builder.AddServerFunction<IAnalyticsFunctions, AnalyticsFunctions>();
builder.Server(services => services
    .AddSingleton(new TrafficModel(2026))
    .AddSingleton<AnalyticsFeed>()
    .AddHostedService<AnalyticsTicker>());

var app = builder.Build();
app.MapAioros();
return app.RunAioros();
