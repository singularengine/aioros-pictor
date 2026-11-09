using System.Collections.ObjectModel;
using Aioros.Pictor.Specs;
using Aioros.Pictor.Svg;
using Aioros.Pictor.Tests.Desktop;
using Aioros.Runtime.Rendering;
using Avalonia.Data;

namespace Aioros.Pictor.Tests.Web;

public sealed class BoundPartTests(DesktopSession desktop)
{
    private static readonly MemberLookup Members = static (owner, member) => item => owner.GetProperty(member)?.GetValue(item);

    [Fact]
    [Trait("AC", "WEB-06")]
    public async Task ABoundSeriesColorIsFollowedByThePlanAsync()
    {
        var (slot, items, red) = await desktop.Session.Dispatch(
            () =>
            {
                var model = new BoundChartModel();
                var view = new BoundChartView { DataContext = model };
                var chart = view.LineChart;
                var slot = chart.WrittenSlot!;
                model.Color = "#ff0000";
                var spec = chart.Spec(500, 300);
                return (slot, model.Items, spec);
            },
            TestContext.Current.CancellationToken);
        var part = Assert.Single(slot.Parts);
        var render = slot.Create(slot.Argument, Members);
        var renderer = Assert.IsType<SlotRender>(render);

        var first = Slots.Render(renderer, items, new Dictionary<string, object?> { [part.Key] = "#ff0000" });
        var again = Slots.Render(renderer, items, new Dictionary<string, object?> { [part.Key] = "#ff0000" });
        var second = Slots.Render(renderer, items, new Dictionary<string, object?> { [part.Key] = "#00ff00" });
        var expected = new System.Buffers.ArrayBufferWriter<byte>();
        ChartSvg.Write(expected, red, items, (owner, member) => Members(owner, member));

        Assert.Equal(SlotPartKind.Read, part.Kind);
        Assert.IsType<Line>(part.Target);
        Assert.Equal("Stroke", part.Property);
        Assert.Contains("\"Holes\"", slot.Argument, StringComparison.Ordinal);
        Assert.Equal(System.Text.Encoding.UTF8.GetString(expected.WrittenSpan), first.Html);
        Assert.Equal(first.Html, again.Html);
        Assert.Contains("stroke=\"#00ff00\"", second.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("#ff0000", second.Html, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "WEB-07")]
    public async Task ABoundPieDataAndTitleAreFollowedByThePlanAsync()
    {
        var slot = await desktop.Session.Dispatch(
            () =>
            {
                var model = new PieModel();
                var pie = new Pie { DataKey = "Value", NameKey = "Name" };
                var chart = new PieChart { Width = 400, Height = 400, DataContext = model };
                pie.Bind(Pie.DataProperty, new Binding(nameof(PieModel.Slices)) { Source = model });
                chart.Bind(Chart.TitleProperty, new Binding(nameof(PieModel.Caption)));
                chart.Elements.Add(pie);
                return chart.WrittenSlot!;
            },
            TestContext.Current.CancellationToken);
        var render = slot.Create(slot.Argument, Members);
        var data = slot.Parts.Single(part => part.Property == "Data").Key;
        var title = slot.Parts.Single(part => part.Property == "Title").Key;
        var slices = new ObservableCollection<BoundItem>([new("A", 1), new("B", 1)]);

        var halves = Slots.Render(render, null, new Dictionary<string, object?> { [data] = slices, [title] = "Halves" });
        slices.Add(new BoundItem("C", 2));
        var thirds = Slots.Render(render, null, new Dictionary<string, object?> { [data] = slices, [title] = "Shares" });

        Assert.Equal(2, Count(halves.Html, "class=\"recharts-sector\""));
        Assert.Contains("aria-label=\"Halves\"", halves.Html, StringComparison.Ordinal);
        Assert.Equal(3, Count(thirds.Html, "class=\"recharts-sector\""));
        Assert.Contains("aria-label=\"Shares\"", thirds.Html, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "WEB-08")]
    public async Task ABoundPropertyNoHoleCanHoldIsLeftForThePlanToRefuseAsync()
    {
        var parts = await desktop.Session.Dispatch(
            () =>
            {
                var model = new PaddedModel();
                var chart = new BarChart { Width = 500, Height = 300, DataContext = model };
                var stop = new Avalonia.Media.GradientStop { Offset = 1 };
                stop.Bind(Avalonia.Media.GradientStop.ColorProperty, new Binding(nameof(PaddedModel.Stop)) { Source = model });
                var gradient = new Avalonia.Media.LinearGradientBrush();
                gradient.GradientStops.Add(new Avalonia.Media.GradientStop(Avalonia.Media.Colors.White, 0));
                gradient.GradientStops.Add(stop);
                chart.Bind(Chart.ReduceMotionProperty, new Binding(nameof(PaddedModel.Still)));
                chart.Elements.Add(new XAxis { DataKey = "Name" });
                chart.Elements.Add(new Bar { DataKey = "Value", Fill = gradient });
                return chart.WrittenSlot!.Parts;
            },
            TestContext.Current.CancellationToken);

        var part = Assert.Single(parts);
        Assert.Equal("ReduceMotion", part.Property);
        Assert.DoesNotContain(parts, declared => declared.Property == "Color");
    }

    [Fact]
    [Trait("AC", "BRUSH-12")]
    public async Task ABoundBrushWindowIsFollowedByThePlanAsync()
    {
        var (slot, items, kept) = await desktop.Session.Dispatch(
            () =>
            {
                var model = new BoundBrushModel();
                var view = new BoundBrushView { DataContext = model };
                return (view.BarChart.WrittenSlot!, model.Items, (model.Start, model.End));
            },
            TestContext.Current.CancellationToken);
        var render = slot.Create(slot.Argument, Members);
        var start = slot.Parts.Single(part => part.Property == "StartIndex");
        var end = slot.Parts.Single(part => part.Property == "EndIndex");

        var narrow = Slots.Render(render, items, new Dictionary<string, object?> { [start.Key] = 1, [end.Key] = 2 });
        var wide = Slots.Render(render, items, new Dictionary<string, object?> { [start.Key] = 0, [end.Key] = 3 });

        Assert.Equal((SlotPartKind.Number, "brush-start", 0m), (start.Kind, start.Key, start.Minimum));
        Assert.Equal((SlotPartKind.Number, "brush-end", 0m), (end.Kind, end.Key, end.Minimum));
        Assert.Contains("\"/Brush/StartIndex\"", slot.Argument, StringComparison.Ordinal);
        Assert.Equal((1, 2), kept);
        Assert.Equal(2, Count(narrow.Html, "class=\"recharts-layer recharts-bar-rectangle\""));
        Assert.Equal(4, Count(wide.Html, "class=\"recharts-layer recharts-bar-rectangle\""));
    }

    [Fact]
    [Trait("AC", "BINDING-08")]
    public async Task BoundChildPropertiesOfEveryFollowedKindAreFollowedOnTheWebAsync()
    {
        var items = new List<BoundItem> { new("A", 4), new("B", 9), new("C", 2), new("D", 7) };
        var (slot, expected) = await desktop.Session.Dispatch(
            () =>
            {
                var model = new StyledModel();
                var chart = Styled(model, items);
                var slot = chart.WrittenSlot!;
                model.Width = 4;
                model.Dots = false;
                model.Curve = CurveType.Step;
                model.Caption = "Visits";
                model.Ticks = 3;
                model.Hidden = true;
                var spec = chart.Spec(500, 300);
                var buffer = new System.Buffers.ArrayBufferWriter<byte>();
                ChartSvg.Write(buffer, spec, items, (owner, member) => Members(owner, member));
                return (slot, System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan));
            },
            TestContext.Current.CancellationToken);
        var render = slot.Create(slot.Argument, Members);
        object Key(string property) => slot.Parts.Single(part => part.Property == property).Key;
        var values = new Dictionary<string, object?>
        {
            [(string)Key("StrokeWidth")] = 4d,
            [(string)Key("Dot")] = false,
            [(string)Key("Type")] = CurveType.Step,
            [(string)Key("Name")] = "Visits",
            [(string)Key("TickCount")] = 3,
            [(string)Key("Hide")] = true,
        };

        var written = Slots.Render(render, items, values);

        Assert.Equal(6, slot.Parts.Length);
        Assert.All(slot.Parts, part => Assert.Equal(SlotPartKind.Read, part.Kind));
        Assert.Equal(expected, written.Html);
        Assert.Contains("stroke-width=\"4\"", written.Html, StringComparison.Ordinal);
    }

    private static LineChart Styled(StyledModel model, List<BoundItem> items)
    {
        var chart = new LineChart { Width = 500, Height = 300, DataContext = model, Data = items };
        var line = new Line { DataKey = "Value" };
        line.Bind(Series.StrokeWidthProperty, new Binding(nameof(StyledModel.Width)));
        line.Bind(PointSeries.DotProperty, new Binding(nameof(StyledModel.Dots)));
        line.Bind(PointSeries.TypeProperty, new Binding(nameof(StyledModel.Curve)));
        line.Bind(Series.NameProperty, new Binding(nameof(StyledModel.Caption)));
        var y = new YAxis();
        y.Bind(Axis.TickCountProperty, new Binding(nameof(StyledModel.Ticks)));
        var x = new XAxis { DataKey = "Name" };
        x.Bind(Axis.HideProperty, new Binding(nameof(StyledModel.Hidden)));
        chart.Elements.Add(x);
        chart.Elements.Add(y);
        chart.Elements.Add(new Legend());
        chart.Elements.Add(line);
        return chart;
    }

    private static int Count(string html, string text) => (html.Length - html.Replace(text, string.Empty, StringComparison.Ordinal).Length) / text.Length;

    public sealed class StyledModel : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        public double Width { get; set { field = value; Changed(); } } = 1;

        public bool? Dots { get; set { field = value; Changed(); } } = true;

        public CurveType Curve { get; set { field = value; Changed(); } } = CurveType.Linear;

        public string? Caption { get; set { field = value; Changed(); } } = "Value";

        public int Ticks { get; set { field = value; Changed(); } } = 5;

        public bool Hidden { get; set { field = value; Changed(); } }

        private void Changed([System.Runtime.CompilerServices.CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
    }

    public sealed class PieModel
    {
        public IReadOnlyList<BoundItem> Slices { get; } = [new("A", 1)];

        public string Caption { get; } = "Traced";
    }

    public sealed class PaddedModel
    {
        public Avalonia.Media.Color Stop { get; } = Avalonia.Media.Colors.SteelBlue;

        public bool Still { get; }
    }
}
