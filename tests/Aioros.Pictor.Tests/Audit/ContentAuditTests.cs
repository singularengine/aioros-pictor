using Aioros.Pictor.Demo;
using Aioros.Pictor.Layout;
using Aioros.Pictor.Specs;
using Aioros.Pictor.Tests.Desktop;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Styling;

namespace Aioros.Pictor.Tests.Audit;

public sealed class ContentAuditTests(DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "AUDIT-22")]
    public async Task ABrushBoundToAPayloadMemberIsRefusedWithItsReasonAsync()
    {
        var example = await Reference.ExampleAsync("SimpleBarChart");

        var refused = await desktop.Session.Dispatch(() => Spec(example, static (chart, templates) => chart.Elements.OfType<Bar>().First().ShapeTemplate = templates["PayloadColorBar"]).Series[0].ShapeContent!.Refused, TestContext.Current.CancellationToken);

        Assert.StartsWith("Bar.ShapeTemplate cannot be written as SVG: the Fill of the template's Rectangle is bound and gives no brush", refused, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "AUDIT-23")]
    public async Task ATemplatesTranslucentColorsKeepTheirAlphaAsync()
    {
        var example = await Reference.ExampleAsync("SimpleBarChart");

        var (bar, tooltip, gray) = await desktop.Session.Dispatch(
            () =>
            {
                var barChart = Made(example, static (chart, templates) => chart.Elements.OfType<Bar>().First().ShapeTemplate = templates["TranslucentBar"]);
                var barSpec = SpecJson.Read(barChart.WrittenSlot!.Argument);
                var tipSpec = Spec(example, static (chart, templates) => chart.Elements.OfType<Tooltip>().Single().ContentTemplate = templates["TranslucentTooltip"]);
                using var bitmap = DesktopRender.Draw(barChart);
                var pixels = DesktopRender.Pixels(bitmap);
                var rect = ((ChartLayout)Painting.Plots.Compute(barSpec, Pictor.Data.ChartData.From(example.Data, static (_, _) => null))).Series[0].Bars[2];
                var offset = ((((int)(rect.Y + (rect.Height / 2))) * pixels.Width) + (int)(rect.X + (rect.Width / 2))) * 4;
                return (Reference.Pictor(example with { Spec = barSpec }), Reference.Pictor(example with { Spec = tipSpec }), pixels.Bgra[offset + 1]);
            },
            TestContext.Current.CancellationToken);

        Assert.Contains("fill=\"rgba(0,0,0,0.502)\"", bar, StringComparison.Ordinal);
        Assert.Contains("background:rgba(0,0,0,0.502)", tooltip, StringComparison.Ordinal);
        Assert.Contains("color:rgba(255,0,0,0.502)", tooltip, StringComparison.Ordinal);
        Assert.InRange(gray, 110, 145);
    }

    [Fact]
    [Trait("AC", "AUDIT-24")]
    public async Task ACalcLiteralTakesASignedExponentAndAsciiDigitsOnlyAsync()
    {
        var example = await Reference.ExampleAsync("SimpleBarChart");

        var refused = await desktop.Session.Dispatch(() => Spec(example, static (chart, templates) => chart.Elements.OfType<Bar>().First().ShapeTemplate = templates["ExponentBar"]).Series[0].ShapeContent!.Refused, TestContext.Current.CancellationToken);

        Assert.Null(refused);
        Assert.Equal("Value*1e-3", new Calc("Value*1e-3").Expression);
        Assert.Equal("2.5E+1 - 2.5e-1", new Calc("2.5E+1 - 2.5e-1").Expression);
    }

    [Fact]
    [Trait("AC", "AUDIT-25")]
    public async Task AVisibilityOverTwoRangesIsRefusedAndASmallThresholdIsKeptAsync()
    {
        var treemap = await Reference.ExampleAsync("SimpleTreemap");
        var bars = await Reference.ExampleAsync("SimpleBarChart");

        var (refused, condition) = await desktop.Session.Dispatch(
            () => (
                Spec(treemap, static (chart, templates) => ((Treemap)chart).ContentTemplate = templates["OneOrDeepNode"]).Treemap!.Content!.Refused,
                Spec(bars, static (chart, templates) => chart.Elements.OfType<Bar>().First().ShapeTemplate = templates["TinyValueBar"]).Series[0].ShapeContent!.Nodes[0].When.Single()),
            TestContext.Current.CancellationToken);

        Assert.Contains("is not one condition on its members", refused, StringComparison.Ordinal);
        Assert.Equal(-1e-7, condition.Value.Constant, 12);
        Assert.False(condition.Inclusive);
    }

    [Fact]
    [Trait("AC", "AUDIT-26")]
    public async Task RedefiningThePaletteRecolorsItsIndexedKeysAsync()
    {
        var (overridden, plain) = await desktop.Session.Dispatch(
            () =>
            {
                var probe = new TextBlock();
                var window = new Window { Content = probe, RequestedThemeVariant = ThemeVariant.Light };
                window.Styles.Add(new PictorTheme());
                window.Show();
                var before = probe.TryFindResource(PictorKeys.PaletteColor(2), ThemeVariant.Light, out var first) ? first : null;
                window.Resources[PictorKeys.Palette] = "#111111 #222222 #333333";
                var after = probe.TryFindResource(PictorKeys.PaletteColor(2), ThemeVariant.Light, out var second) ? second : null;
                var beyond = probe.TryFindResource(PictorKeys.PaletteColor(4), ThemeVariant.Light, out _);
                window.Close();
                return ((after, beyond), before);
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(("#222222", false), overridden);
        Assert.Equal(Theming.Palettes.Split(Theming.ChartTheme.Light.Palette)[1], plain);
    }

    [Fact]
    [Trait("AC", "AUDIT-28")]
    public async Task ABoundValueTheMarkerLeavesUnseenButAnotherValueChangesIsNotFollowedAsync()
    {
        var example = await Reference.ExampleAsync("SimpleLineChart");

        var (clamped, copied) = await desktop.Session.Dispatch(
            () =>
            {
                var chart = SpecControls.Create(example);
                var line = chart.Elements.OfType<Line>().First();
                line.Bind(Series.StrokeWidthProperty, new Binding("Width") { Source = new StrokeModel() });
                var other = chart.Elements.OfType<Line>().Last();
                other.Bind(Series.StrokeWidthProperty, new Binding("Width") { Source = new StrokeModel() });
                ChartSpec Build()
                {
                    var spec = chart.Spec(example.Spec.Width, example.Spec.Height);
                    return spec with { Series = [spec.Series[0] with { StrokeWidth = Math.Min(spec.Series[0].StrokeWidth, 10) }, .. spec.Series[1..]] };
                }

                var parts = BoundParts.Find(chart, Build, Chart.Members, static (target, property, key) => Aioros.Runtime.Rendering.SlotPart.Read(key, target, property.Name), static (_, _) => null);
                return (parts.Any(part => ReferenceEquals(part.Part.Target, line)), parts.Any(part => ReferenceEquals(part.Part.Target, other) && !part.Holes.IsEmpty));
            },
            TestContext.Current.CancellationToken);

        Assert.False(clamped);
        Assert.True(copied);
    }

    [Fact]
    [Trait("AC", "AUDIT-29")]
    public async Task OnlyADataBindingAtTheLocalPriorityIsAPartAsync()
    {
        var example = await Reference.ExampleAsync("SimpleLineChart");

        var found = await desktop.Session.Dispatch(
            () =>
            {
                var chart = SpecControls.Create(example);
                var lines = chart.Elements.OfType<Line>().ToList();
                var model = new StrokeModel();
                lines[0].Bind(Series.StrokeWidthProperty, new Binding("Width") { Source = model });
                lines[1].Bind(Series.StrokeWidthProperty, new MultiBinding { Bindings = { new Binding("Width") { Source = model } }, Converter = new Avalonia.Data.Converters.FuncMultiValueConverter<object?, double>(static values => 3) });
                var axis = chart.Elements.OfType<XAxis>().First();
                axis.Bind(Axis.TickCountProperty, new Fixed(9));
                var parts = BoundParts.Find(chart, () => chart.Spec(example.Spec.Width, example.Spec.Height), Chart.Members, static (target, property, key) => Aioros.Runtime.Rendering.SlotPart.Read(key, target, property.Name), static (_, _) => null);
                return parts.Select(part => part.Part.Target.GetType().Name + "." + part.Part.Property + (ReferenceEquals(part.Part.Target, lines[0]) ? " first" : string.Empty)).ToList();
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(["Line.StrokeWidth first"], found);
    }

    private static Chart Made(RechartsExample example, Action<Chart, AuditTemplates> templates)
    {
        var chart = SpecControls.Create(example);
        templates(chart, new AuditTemplates());
        chart.Measure(new Size(example.Spec.Width, example.Spec.Height));
        chart.Arrange(new Rect(0, 0, example.Spec.Width, example.Spec.Height));
        return chart;
    }

    private static ChartSpec Spec(RechartsExample example, Action<Chart, AuditTemplates> templates) => SpecJson.Read(Made(example, templates).WrittenSlot!.Argument);

    private sealed class Fixed(int value) : IObservable<object?>
    {
        public IDisposable Subscribe(IObserver<object?> observer)
        {
            observer.OnNext(value);
            return new Nothing();
        }
    }

    private sealed class Nothing : IDisposable
    {
        public void Dispose()
        {
        }
    }

    public sealed class StrokeModel
    {
        public double Width { get; set; } = 10;

        public int Count { get; set; } = 7;
    }
}
