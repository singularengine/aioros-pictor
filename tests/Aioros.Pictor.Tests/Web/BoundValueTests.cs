using System.ComponentModel;
using System.Runtime.CompilerServices;
using Aioros.Pictor.BoundValues;
using Aioros.Pictor.Svg;
using Aioros.Pictor.Tests.Desktop;
using Aioros.Runtime.Rendering;
using Avalonia.Data;
using Color = Avalonia.Media.Color;
using GradientStop = Avalonia.Media.Immutable.ImmutableGradientStop;
using LinearGradient = Avalonia.Media.Immutable.ImmutableLinearGradientBrush;
using SolidBrush = Avalonia.Media.Immutable.ImmutableSolidColorBrush;

namespace Aioros.Pictor.Tests.Web;

public sealed class BoundValueTests(DesktopSession desktop)
{
    private static readonly MemberLookup Members = static (owner, member) => item => owner.GetProperty(member)?.GetValue(item);

    private static readonly List<BoundItem> Items = [new("A", 4), new("B", 9), new("C", 2), new("D", 7)];

    [Fact]
    [Trait("AC", "BINDING-09")]
    public async Task BoundColorsAndBrushesAreFollowedOnTheWebAsync()
    {
        var (slot, holes, expected, values) = await desktop.Session.Dispatch(
            () =>
            {
                var model = new PaintModel();
                var chart = new BarChart { Width = 500, Height = 300, DataContext = model, Data = Items };
                chart.Elements.Add(new XAxis { DataKey = "Name" });
                foreach (var path in (string[])[nameof(PaintModel.Accent), nameof(PaintModel.Wash), nameof(PaintModel.Shade)])
                {
                    var bar = new Bar { DataKey = "Value" };
                    bar.Bind(Series.FillProperty, new Binding(path));
                    chart.Elements.Add(bar);
                }

                var tinted = new Avalonia.Media.SolidColorBrush();
                tinted.Bind(Avalonia.Media.SolidColorBrush.ColorProperty, new Binding(nameof(PaintModel.Tint)) { Source = model });
                chart.Elements.Add(new Bar { DataKey = "Value", Fill = tinted });
                var slot = chart.WrittenSlot!;
                var holes = Specs(slot.Argument);
                model.Accent = Color.FromRgb(0x22, 0xaa, 0x44);
                model.Wash = new LinearGradient([new GradientStop(0, Color.FromRgb(0xff, 0x73, 0x00)), new GradientStop(1, Color.FromArgb(0x40, 0xff, 0x73, 0x00))], startPoint: new Avalonia.RelativePoint(0, 0, Avalonia.RelativeUnit.Relative), endPoint: new Avalonia.RelativePoint(0, 1, Avalonia.RelativeUnit.Relative));
                model.Shade = new SolidBrush(Color.FromRgb(0x33, 0x66, 0x99), 0.5);
                model.Tint = Color.FromRgb(0x99, 0x33, 0x66);
                return (slot, holes, Write(chart), new object?[] { model.Accent, model.Wash, model.Shade, model.Tint });
            },
            TestContext.Current.CancellationToken);
        var parts = slot.Parts;

        var written = Slots.Render(slot.Create(slot.Argument, Members), Items, Enumerable.Range(0, 4).ToDictionary(i => parts[i].Key, i => values[i]));

        Assert.Equal(typeof(BoundChartRenderer).FullName, slot.Writer);
        Assert.Equal(4, parts.Length);
        Assert.All(holes, hole => Assert.Equal(HoleKind.Paint, hole.Kind));
        Assert.Equal(expected, written.Html);
        Assert.Contains("fill=\"#22aa44\"", written.Html, StringComparison.Ordinal);
        Assert.Contains("fill=\"#33669980\"", written.Html, StringComparison.Ordinal);
        Assert.Equal(1, Interactive.WebChart.Count(written.Html, "<linearGradient "));
        Assert.Contains("fill=\"#993366\"", written.Html, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "BINDING-10")]
    public async Task BoundThicknessesAreFollowedOnTheWebAsync()
    {
        static LineChart Padded(PaddingModel model)
        {
            var chart = new LineChart { Width = 500, Height = 300, DataContext = model, Data = Items };
            chart.Bind(Chart.PaddingProperty, new Binding(nameof(PaddingModel.Inner)));
            var x = new XAxis { DataKey = "Name" };
            x.Bind(Axis.PaddingProperty, new Binding(nameof(PaddingModel.Axis)));
            var brush = new Brush { DataKey = "Name" };
            brush.Bind(Brush.PaddingProperty, new Binding(nameof(PaddingModel.Window)));
            chart.Elements.Add(x);
            chart.Elements.Add(new YAxis());
            chart.Elements.Add(new Line { DataKey = "Value" });
            chart.Elements.Add(brush);
            return chart;
        }

        var (slot, holding, holes, values) = await desktop.Session.Dispatch(
            () =>
            {
                var slot = Padded(new PaddingModel()).WrittenSlot!;
                var model = new PaddingModel { Inner = new Avalonia.Thickness(30, 12, 18, 6), Axis = new Avalonia.Thickness(24, 0, 16, 0), Window = new Avalonia.Thickness(4, 2, 6, 8) };
                return (slot, Padded(model).WrittenSlot!, Specs(slot.Argument), (model.Inner, model.Axis, model.Window));
            },
            TestContext.Current.CancellationToken);
        var bound = slot.Parts.Where(part => part.Property == "Padding").ToDictionary(part => part.Key, part => (object?)(part.Target switch { LineChart => values.Inner, XAxis => values.Axis, _ => values.Window }));

        var written = Slots.Render(slot.Create(slot.Argument, Members), Items, bound);
        var expected = Slots.Render(holding.Create(holding.Argument, Members), Items, new Dictionary<string, object?>());

        Assert.Equal(3, bound.Count);
        Assert.Equal(["Bottom", "Left", "Right", "Top"], HoleMembers(holes, slot.Parts.Single(part => part.Target is LineChart && part.Property == "Padding").Key));
        Assert.Equal(["Left", "Right"], HoleMembers(holes, slot.Parts.Single(part => part.Target is XAxis && part.Property == "Padding").Key));
        Assert.Equal(["Bottom", "Left", "Right", "Top"], HoleMembers(holes, slot.Parts.Single(part => part.Target is Brush && part.Property == "Padding").Key));
        Assert.All(holes, hole => Assert.Equal(HoleKind.Number, hole.Kind));
        Assert.NotEqual(expected.Html, Slots.Render(slot.Create(slot.Argument, Members), Items, new Dictionary<string, object?>()).Html);
        Assert.Equal(expected.Html, written.Html);
    }

    [Fact]
    [Trait("AC", "BINDING-11")]
    public async Task BoundCornerRadiiAndNumberListsAreFollowedOnTheWebAsync()
    {
        var (slot, holes, expected, values, radii) = await desktop.Session.Dispatch(
            () =>
            {
                var model = new RadiusModel();
                var chart = new BarChart { Width = 500, Height = 300, DataContext = model, Data = Items };
                var bar = new Bar { DataKey = "Value", Background = "#eee" };
                bar.Bind(Bar.RadiusProperty, new Binding(nameof(RadiusModel.Corners)));
                bar.Bind(Bar.BackgroundRadiusProperty, new Binding(nameof(RadiusModel.Rounding)));
                var area = new ReferenceArea { X1 = "B", X2 = "C" };
                area.Bind(ReferenceArea.RadiusProperty, new Binding(nameof(RadiusModel.Text)));
                chart.Elements.Add(new XAxis { DataKey = "Name" });
                chart.Elements.Add(new YAxis());
                chart.Elements.Add(bar);
                chart.Elements.Add(area);
                var slot = chart.WrittenSlot!;
                var holes = Specs(slot.Argument);
                model.Corners = new Avalonia.CornerRadius(8, 6, 0, 2);
                model.Rounding = [3, 3, 0, 0];
                model.Text = "4";
                var spec = chart.Spec(500, 300).Series[0];
                return (slot, holes, Write(chart), new object?[] { model.Corners, model.Rounding, model.Text }, (spec.Radius, spec.BackgroundRadius));
            },
            TestContext.Current.CancellationToken);
        var radius = slot.Parts.Single(part => part.Target is Bar && part.Property == "Radius").Key;
        var background = slot.Parts.Single(part => part.Property == "BackgroundRadius").Key;
        var area = slot.Parts.Single(part => part.Target is ReferenceArea).Key;

        var written = Slots.Render(slot.Create(slot.Argument, Members), Items, new Dictionary<string, object?> { [radius] = values[0], [background] = values[1], [area] = values[2] });

        Assert.Equal(3, slot.Parts.Length);
        Assert.All(holes, hole => Assert.Equal(HoleKind.Numbers, hole.Kind));
        Assert.Equal([8, 6, 0, 2], radii.Radius);
        Assert.Equal([3, 3, 0, 0], radii.BackgroundRadius);
        Assert.Equal([4], SpecText.Numbers((object)4d));
        Assert.Equal(expected, written.Html);
    }

    [Fact]
    [Trait("AC", "BINDING-12")]
    public async Task BoundDashPatternsAreFollowedOnTheWebAsync()
    {
        var (slot, holes, expected, values) = await desktop.Session.Dispatch(
            () =>
            {
                var model = new DashModel();
                var chart = new LineChart { Width = 500, Height = 300, DataContext = model, Data = Items };
                var grid = new CartesianGrid();
                grid.Bind(CartesianGrid.StrokeDashArrayProperty, new Binding(nameof(DashModel.Grid)));
                var line = new Line { DataKey = "Value" };
                line.Bind(Series.StrokeDashArrayProperty, new Binding(nameof(DashModel.Line)));
                chart.Elements.Add(grid);
                chart.Elements.Add(new XAxis { DataKey = "Name" });
                chart.Elements.Add(line);
                var slot = chart.WrittenSlot!;
                var holes = Specs(slot.Argument);
                model.Line = "8 4";
                model.Grid = new Avalonia.Collections.AvaloniaList<double> { 2, 6 };
                return (slot, holes, Write(chart), (model.Line, model.Grid));
            },
            TestContext.Current.CancellationToken);
        var line = slot.Parts.Single(part => part.Target is Line).Key;
        var grid = slot.Parts.Single(part => part.Target is CartesianGrid).Key;

        var written = Slots.Render(slot.Create(slot.Argument, Members), Items, new Dictionary<string, object?> { [line] = values.Line, [grid] = values.Grid });

        Assert.Equal(2, holes.Length);
        Assert.All(holes, hole => Assert.Equal(HoleKind.Dashes, hole.Kind));
        Assert.Equal(expected, written.Html);
        Assert.Contains("stroke-dasharray=\"8 4\"", written.Html, StringComparison.Ordinal);
        Assert.Contains("stroke-dasharray=\"2 6\"", written.Html, StringComparison.Ordinal);
    }

    private static string Write(Chart chart)
    {
        var buffer = new System.Buffers.ArrayBufferWriter<byte>();
        ChartSvg.Write(buffer, chart.Spec(500, 300), chart.Data, (owner, member) => Members(owner, member));
        return System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static SpecHole[] Specs(string argument) =>
        [.. System.Text.Json.JsonSerializer.Deserialize<SpecArgumentView>(argument, Json)!.Holes];

    private static string?[] HoleMembers(SpecHole[] holes, string key) => [.. holes.Where(hole => hole.Key == key).Select(hole => hole.Member).Order(StringComparer.Ordinal)];

    private static readonly System.Text.Json.JsonSerializerOptions Json = new() { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } };

    private sealed record SpecArgumentView(SpecHole[] Holes);

    public sealed class PaintModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public Color Accent { get; set { field = value; Changed(); } } = Color.FromRgb(0x88, 0x84, 0xd8);

        public Avalonia.Media.IBrush Wash { get; set { field = value; Changed(); } } = new SolidBrush(Color.FromRgb(0x33, 0x66, 0x99), 0.5);

        public Avalonia.Media.IBrush Shade { get; set { field = value; Changed(); } } = new LinearGradient([new GradientStop(0, Color.FromRgb(0x82, 0xca, 0x9d)), new GradientStop(1, Color.FromRgb(0xff, 0xff, 0xff))]);

        public Color Tint { get; set { field = value; Changed(); } } = Color.FromRgb(0xff, 0xc6, 0x58);
    }

    public sealed class PaddingModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public Avalonia.Thickness Inner { get; set { field = value; Changed(); } } = new(5);

        public Avalonia.Thickness Axis { get; set { field = value; Changed(); } } = new(10, 0, 10, 0);

        public Avalonia.Thickness Window { get; set { field = value; Changed(); } } = new(1);
    }

    public sealed class RadiusModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public Avalonia.CornerRadius Corners { get; set { field = value; Changed(); } } = new(4, 4, 0, 0);

        public double[] Rounding { get; set { field = value; Changed(); } } = [2];

        public string Text { get; set { field = value; Changed(); } } = "0";
    }

    public sealed class DashModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public object Line { get; set { field = value; Changed(); } } = new Avalonia.Collections.AvaloniaList<double> { 5, 3 };

        public object Grid { get; set { field = value; Changed(); } } = "3 3";
    }
}
