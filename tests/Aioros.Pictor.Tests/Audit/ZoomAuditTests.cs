using System.Text.RegularExpressions;
using Aioros.Pictor.Tests.Desktop;
using Aioros.Pictor.Tests.Interactive;
using Aioros.Runtime.Rendering;
using Avalonia.Data;

namespace Aioros.Pictor.Tests.Audit;

public sealed partial class ZoomAuditTests(DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "AUDIT-36")]
    public async Task ABoundZoomIsAChoiceFieldOfTheNodesAsync()
    {
        var example = await Reference.ExampleAsync("NestedTreemap");

        var (parts, written, commanded) = await desktop.Session.Dispatch(
            () =>
            {
                var model = new ZoomModel { Zoom = "0" };
                var chart = (Treemap)WebChart.Create(example);
                chart.Bind(Treemap.ZoomProperty, new Binding(nameof(ZoomModel.Zoom)) { Source = model, Mode = BindingMode.TwoWay });
                var html = WebChart.Write(chart, example.Spec.Width, example.Spec.Height);
                return (chart.WrittenSlot!.Parts, html, WebChart.Write((Treemap)WebChart.Create(example), example.Spec.Width, example.Spec.Height));
            },
            TestContext.Current.CancellationToken);
        var options = written.Reserved.Where(reserved => reserved.Key == "zoom-path").ToList();

        Assert.Contains(parts, part => part.Kind == SlotPartKind.Choice && part.Key == "zoom-path" && part.Property == nameof(Treemap.Zoom));
        Assert.DoesNotContain(parts, part => part.Kind == SlotPartKind.Command && part.Key == "zoom");
        Assert.Single(options.Select(option => option.Id).Distinct());
        Assert.Contains(options, option => option.Parameter == "0");
        Assert.Contains(options, option => option.Parameter == string.Empty);
        Assert.Matches("<input type=\"radio\" hidden checked aria-label=\"axis\"", written.Html);
        Assert.Equal(1, Regex.Count(written.Html, " checked"));
        Assert.Equal(2, Regex.Count(written.Html, "<input type=\"radio\" class=\"pictor-zoom\""));
        Assert.DoesNotContain("<button", written.Html, StringComparison.Ordinal);
        Assert.Contains("name=\"_btn\"", commanded.Html, StringComparison.Ordinal);
    }

    public sealed class ZoomModel
    {
        public string? Zoom { get; set; }
    }
}
